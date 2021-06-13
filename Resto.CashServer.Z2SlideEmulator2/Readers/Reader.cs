using LicenseClient;
using Resto.Framework.Common;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ZPort;
using ZREADER;


namespace Resto.CashServer.Z2SlideEmulator.Readers
{
    class Reader
    {
        const ZP_PORT_TYPE RdPortType = ZP_PORT_TYPE.ZP_PORT_COM;
        //const string RdPortName = "COM3";

        string[] ReaderTypeStrs = { "Неизвестно", "Z-2 USB", "Matrix III Rd-All", "Z-2 USB MF", "Matrix III Net", "CP-Z-2MF", "Z-2 EHR", "Z-2 Base", "Matrix V" };
        string[] CardTypeStrs = { "Неизвестно", "EM", "HID", "IC", "UL", "1K", "4K", "DF", "PX",
                                         "Cod433 Fix", "Cod433", "Dallas", "CAME", "Plus", "Plus 1K", "Plus 2K", "Plus 4K", "Mini" };

        static IntPtr m_hRd;
        static readonly Byte[] MfAuthKey = new Byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };


        //Logger logger = LogManager.GetCurrentClassLogger();
        static private Logger logger = new LoggerConfiguration()
           .MinimumLevel.Verbose()
           .WriteTo.File(
             string.Format(@"{0}\iiko\CashServer\Logs\CardSlide_Emulator.log", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)),
             rollingInterval: RollingInterval.Day)
           .CreateLogger();
        static LicenseManager lm = new LicenseManager();

        ManualResetEvent m_oEvent = null;

        System.Timers.Timer m_timer = null;
        //#region markers
        //private void Yellow() { status.BackColor = Color.Yellow; }
        //private void Green() { status.BackColor = Color.LightGreen; }
        //private void Red() { status.BackColor = Color.Red; }
        //#endregion

        //считывание данных из блоков
        [STAThread]
        void DoRead1K4K(Byte[] rNum, ZR_CARD_TYPE nCdType)
        {
            try
            {
                lm.VerefecationLicenseDate();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return;
            }
            int startTick;
            ZRIntf.ZR_Rd_SetCapture(m_hRd);
            try
            {
                int nRet;
                nRet = ZRIntf.ZR_Rd_SelectCard(m_hRd, rNum, nCdType);
                if (nRet < 0)
                {
                    logger.Error("Ошибка ZR_Rd_SelectCard ({0:X4}).", nRet);
                    //Red();
                    //Console.ReadLine();
                    return;
                }
                startTick = Environment.TickCount;
                int nMax = (nCdType == ZR_CARD_TYPE.ZR_CD_4K) ? 256 / 32 : 64 / 8;
                int i, nSectN, nSBlockN, nSBlockMax;
                Byte[] aBuf = new Byte[16]; // 1 блок по 16 байт
                i = 4;
                nMax = 5;
                while (i < nMax) // цикл по блокам
                {
                    if ((nCdType == ZR_CARD_TYPE.ZR_CD_4K) && (i > 127))
                    {
                        nSectN = ((i - 128) / 16);
                        nSBlockN = ((i - 128) % 16);
                        nSBlockMax = 16;
                    }
                    else
                    {
                        nSectN = (i / 4);
                        nSBlockN = (i % 4);
                        nSBlockMax = 4;
                    }

                    if (nSBlockN == 0)
                    {
                        nRet = ZRIntf.ZR_Rd_AuthorizeSect(m_hRd, i, MfAuthKey, 0);
                        if (nRet < 0)
                        {
                            if (nRet == ZRIntf.ZR_E_AUTHORIZE)
                            {
                                logger.Error("Ошибка авторизации сектора №{0:X4}.", nSectN);
                                i += nSBlockMax;
                                continue;
                            }
                            //logger.Fatal("Ошибка ZR_Rd_AuthorizeSect ({0:X4}).", nRet);
                            //Red();
                            //Console.ReadLine();

                            
                            return;
                        }
                        //Console.WriteLine("-----------------------");
                        logger.Debug("Сектор {0}", nSectN);
                    }
                    nRet = ZRIntf.ZR_Rd_ReadMfCardBlock(m_hRd, i, aBuf, 0);
                    if (nRet < 0)
                    {
                        if (nRet == ZRIntf.ZR_E_CARDNACK)
                        {
                            //logger.Error("\t({0}-{1}): Отказ карты.", nSectN, nSBlockN);
                            i++;
                            continue;
                        }
                        logger.Fatal("Ошибка ZR_Rd_ReadMfCardBlock ({0:X4}).", nRet);
                        //Red();
                        //Console.ReadLine();
                        return;
                    }
                    logger.Debug("{0} ({1}-{2}): {3:X2} {4:X2} {5:X2} {6:X2} " +
                        "{7:X2} {8:X2} {9:X2} {10:X2} {11:X2} {12:X2} {13:X2} " +
                        "{14:X2} {15:X2} {16:X2} {17:X2} {18:X2}",
                        i, nSectN, nSBlockN,
                        aBuf[0], aBuf[1], aBuf[2], aBuf[3],
                        aBuf[4], aBuf[5], aBuf[6], aBuf[7],
                        aBuf[8], aBuf[9], aBuf[10], aBuf[11],
                        aBuf[12], aBuf[13], aBuf[14], aBuf[15]);
                    var keyStr = "";
                    //KeyboardSend.KeyDown(System.Windows.Forms.Keys.F4);
                    foreach (var k in aBuf)
                    {
                        if (k == 0) continue;
                        keyStr += (k - '0').ToString();
                        //KeyboardSend.KeyDown((char)(k));
                    }
                    //KeyboardSend.KeyDown(System.Windows.Forms.Keys.Enter);
                    logger.Debug($"Трек карты: {keyStr}");
                    DoWrite(keyStr);
                    

                    if (nSBlockN == (nSBlockMax - 1))
                    {
                        Byte[] aAccBytes = new Byte[4];
                        Array.Copy(aBuf, 6, aAccBytes, 0, 4);
                    }
                    i++;
                }
                logger.Debug("Успешно. ");
            }
            finally
            {
                ZRIntf.ZR_Rd_ReleaseCapture(m_hRd);
            }
            TimeSpan rezultTime = TimeSpan.FromMilliseconds(Environment.TickCount - startTick);
            //logger.Info("Общее время: {0}", rezultTime);
        }

        [STAThread]
        private void DoWrite(string keyStr)
        {
            logger.Debug($"Номер карты: {keyStr}");
          
            Resto.Front.Controllers.CardProcessor.Instance
                .ImitateCardRolled(new Resto.Framework.Common.CardProcessor.MagnetTrackData(string.Empty, keyStr, string.Empty));
        }

        //[STAThread]
        //static void DoReadUL()
        //{
        //    int nRet;
        //    int i, j;
        //    UInt32 n;
        //    Byte[][] aBuf = new Byte[16][];
        //    string s;

        //    for (i = 0; i < 16; i++)
        //    {
        //        if ((i % 4) == 0)
        //        {
        //            aBuf[i] = new Byte[4];
        //            nRet = ZRIntf.ZR_Rd_ReadULCard4Page(m_hRd, i, aBuf[i]);
        //            if (nRet < 0)
        //            {
        //                Console.WriteLine("Ошибка ZR_Rd_ReadULCard4Page ({0}).", nRet);
        //                Console.ReadLine();
        //                return;
        //            }
        //        }
        //        s = String.Format("{0}. {1:X2} {2:X2} {3:X2} {4:X2}",
        //            i, aBuf[i][0], aBuf[i][1], aBuf[i][2], aBuf[i][3]);
        //        switch (i)
        //        {
        //            case 0:
        //            case 1:
        //                Console.WriteLine("{0} SN.", s);
        //                break;
        //            case 2:
        //                Console.WriteLine(s);
        //                n = aBuf[i][2];
        //                Console.WriteLine("Lock0[{0:X2}] BOTP:{1}, BL9-4:{2}, BL15-10:{3}, OTP:{4}, L4:{5}, L5:{6}, L6:{7}, L7:{8}",
        //                    n,
        //                    n & 1,
        //                    (n >> 1) & 1,
        //                    (n >> 2) & 1,
        //                    (n >> 3) & 1,
        //                    (n >> 4) & 1,
        //                    (n >> 5) & 1,
        //                    (n >> 6) & 1,
        //                    (n >> 7) & 1);
        //                n = aBuf[i][3];
        //                Console.WriteLine("Lock1[{0:X2}] L8:{1}, L9:{2}, L10:{3}, L11:{4}, L12:{5}, L13:{6}, L14:{7}, L15:{8}",
        //                    n,
        //                    n & 1,
        //                    (n >> 1) & 1,
        //                    (n >> 2) & 1,
        //                    (n >> 3) & 1,
        //                    (n >> 4) & 1,
        //                    (n >> 5) & 1,
        //                    (n >> 6) & 1,
        //                    (n >> 7) & 1);
        //                break;
        //            case 3: // OTP
        //                Console.WriteLine(s);
        //                StringBuilder builder = new StringBuilder("00000000 00000000 00000000 00000000");
        //                n = Convert.ToUInt32(aBuf[i]);
        //                j = 1;
        //                while (j <= 35)
        //                {
        //                    if ((n & 1) != 0)
        //                        builder[j] = '1';
        //                    n >>= 1;
        //                    j++;
        //                    if ((j % 8) == 0)
        //                        j++;
        //                }
        //                n = Convert.ToUInt32(aBuf[i]);
        //                if (GetBit(aBuf[2][2], 3))
        //                    Console.WriteLine("OTP: {0} [{1}] Locked", builder.ToString(), n.ToString("X4"));
        //                else
        //                    Console.WriteLine("OTP: {0} [{1}]", builder.ToString(), n.ToString("X4"));
        //                break;
        //            default:
        //                n = aBuf[2][2];
        //                n += ((UInt32)aBuf[2][3] << 8);
        //                if (GetBit(n, i))
        //                    s += " Locked";
        //                Console.WriteLine(s);
        //                break;
        //        }
        //    }
        //    Console.WriteLine("Успешно.");
        //}

        [STAThread]
        void DoWriteSR(string serialNumber)
        {
            try
            {
                lm.VerefecationLicenseDate();
                DoWrite(int.Parse(serialNumber, System.Globalization.NumberStyles.HexNumber).ToString());
                //DoWrite(serialNumber);
            }
            catch (OverflowException)
            {
                DoWrite(GetHexToDecStr(serialNumber));
                //doWrite(serialNumber);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }
        [STAThread]
        void CheckNotifyMsgs()
        {
            int hr;
            UInt32 nMsg = 0;
            IntPtr nMsgParam = IntPtr.Zero;
            while ((hr = ZRIntf.ZR_Rd_GetNextMessage(m_hRd, ref nMsg, ref nMsgParam)) == ZRIntf.S_OK)
            {
                switch (nMsg)
                {
                    case ZRIntf.ZR_RN_CARD_INSERT:
                        {
                            ZR_CARD_INFO pInfo = (ZR_CARD_INFO)Marshal.PtrToStructure(nMsgParam, typeof(ZR_CARD_INFO));
                            logger.Debug("Карта поднесена {0} {1}",
                            CardTypeStrs[(int)pInfo.nType],
                               ZRIntf.CardNumToStr(pInfo.nNum, pInfo.nType));

                            ///передача серийного байтового номера для дальнейшего преобразования
                            DoWriteSR(ZRIntf.CardNumToStr(pInfo.nNum, pInfo.nType));


                            //switch (pInfo.nType)
                            //{
                            //    case ZR_CARD_TYPE.ZR_CD_UL:
                            //        //DoReadUL(pInfo.nNum, pInfo.nType);
                            //        break;
                            //    case ZR_CARD_TYPE.ZR_CD_1K:
                            //    case ZR_CARD_TYPE.ZR_CD_4K:
                            //        DoRead1K4K(pInfo.nNum, pInfo.nType);
                            //        break;
                            //    default:
                            //        //logger.Error("Не могу прочитать карту.");
                            //        break;
                            //}
                        }
                        break;
                    case ZRIntf.ZR_RN_CARD_REMOVE:
                        {
                            ZR_CARD_INFO pInfo = (ZR_CARD_INFO)Marshal.PtrToStructure(nMsgParam, typeof(ZR_CARD_INFO));
                            logger.Debug("Удалена карта {0} {1}",
                                 CardTypeStrs[(int)pInfo.nType],
                                 ZRIntf.CardNumToStr(pInfo.nNum, pInfo.nType));
                        }
                        break;
                }
            }
            if (hr == ZPIntf.ZP_S_NOTFOUND)
                hr = ZRIntf.S_OK;
            return;
            //return hr;
        }

        string GetHexToDecStr(string hexInput)
        {
            try
            {
                var length = hexInput.Length;
                var hex = hexInput.Trim();
                var nbyte = 4;
                nbyte *= 2;
                var l = 1;
                double summ = 0;

                do
                {
                    var k = string.Join("", hex.Take(nbyte));

                    hex = hexInput.Remove(0, l * nbyte > length ? length : l * nbyte);
                    var x = long.Parse(k, System.Globalization.NumberStyles.HexNumber);
                    if (hex.Length != 0)
                    {
                        var pow = Math.Pow(16, hex.Length);
                        summ += x * pow;
                    }
                    else
                    {
                        summ += x;
                    }
                    l++;
                }
                while (hex.Length != 0);
                return summ.ToString("r");

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public delegate void ReaderStateHandler();
        public event ReaderStateHandler CheckMsgs;

        [STAThread]
        public void StartNotifyTask()
        {
            try
            {
                string data = string.Empty;
                lm.VerefecationLicense(ref data);
                CheckMsgs += CheckNotifyMsgs;

                m_timer = new System.Timers.Timer(1000);
                m_timer.Elapsed += M_timer_Elapsed;
                m_timer.AutoReset = true;
                m_timer.Start();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }


        }
        [STAThread]
        private void M_timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CheckMsgs();

        }
        [STAThread]
        public void StopNotifyTask()
        {
            try
            {
                if (CheckMsgs is null || m_timer is null)
                {
                    throw new NullReferenceException();
                }
                CheckMsgs -= CheckNotifyMsgs;
                m_timer.Stop();
                m_timer.Elapsed -= M_timer_Elapsed;
                m_timer.Dispose();
            }
            catch (Exception) { }

        }
        [STAThread]
        List<string> EnumSerialPorts()
        {
            List<string> rPorts = null;
            try
            {

                UInt32 nVersion = ZRIntf.ZR_GetVersion();
                UInt32 nVerMajor = (nVersion & 0xFF);
                UInt32 nVerMinor = ((nVersion >> 8) & 0xFF);
                UInt32 nVerBuild = ((nVersion >> 16) & 0xFF);
                logger.Debug("SDK Reader v{0}.{1}.{2}", nVerMajor, nVerMinor, nVerBuild);
                if ((nVerMajor != ZRIntf.ZR_SDK_VER_MAJOR) || (nVerMinor != ZRIntf.ZR_SDK_VER_MINOR))
                {
                    logger.Fatal("Неправильная версия SDK Reader.");
                    //Console.ReadLine();
                    return rPorts;
                }

                int hr;
                hr = ZRIntf.ZR_Initialize(ZPIntf.ZP_IF_NO_MSG_LOOP);
                if (hr < 0)
                {
                    logger.Fatal("Ошибка ZR_Initialize ({0}).", hr);
                    //Console.ReadLine();
                    return rPorts;
                }

                logger.Debug("Перечисление последовательных портов...");
                int nPortCount = 0;
                IntPtr hList = new IntPtr();
                hr = ZRIntf.ZR_GetPortInfoList(ref hList, ref nPortCount);

                if (hr < 0)
                {
                    logger.Fatal("Ошибка ZG_EnumSerialPorts ({0}).", hr);
                    //Console.ReadLine();
                    return rPorts;
                }
                try
                {
                    rPorts = new List<string>();
                    ZP_PORT_INFO rPI = new ZP_PORT_INFO();
                    for (int i = 0; i < nPortCount; i++)
                    {
                        ZPIntf.ZP_GetPortInfo(hList, i, ref rPI);
                        logger.Debug("{0}. {1} ({2}); {3}",
                           (i + 1),
                           rPI.szName,
                           rPI.szFriendly,
                           ((rPI.nFlags & ZPIntf.ZP_PIF_BUSY) != 0) ? "Занят" : "");
                        if ((rPI.nFlags & ZPIntf.ZP_PIF_BUSY) == 0)
                        {
                            rPorts.Add(rPI.szFriendly);
                        }
                    }
                }
                finally
                {
                    ZRIntf.ZR_CloseHandle(hList);
                }
                //Console.WriteLine("--------------");
                if (nPortCount > 0)
                    logger.Debug("Найдено {0} портов.", nPortCount);
                else
                    logger.Error("Порты не найдены.");
                return new List<string>(rPorts);
            }
            catch (ArgumentNullException)
            { return rPorts; }
            catch (DllNotFoundException ex)
            {
                logger.Fatal($"{ex.Message}");
                return rPorts;
            }
            finally
            {
                //StopNotifyThread();
                if (m_hRd != IntPtr.Zero)
                    ZRIntf.ZR_CloseHandle(m_hRd);
                //ZRIntf.ZR_Finalyze();
                //Red();
            }
        }
        [STAThread]
        bool Rd_Open(string PortName)
        {
            int hr;
            ZR_RD_INFO rRdInf = new ZR_RD_INFO();
            logger.Debug("Подключение к считывателю ({0})...", PortName);
            ZR_RD_OPEN_PARAMS rOpen = new ZR_RD_OPEN_PARAMS(RdPortType, PortName);
            hr = ZRIntf.ZR_Rd_Open(ref m_hRd, ref rOpen, ref rRdInf);
            if (hr < 0)
            {
                logger.Fatal("Ошибка ZR_Rd_Open ({0}).", hr);
                //Red();
                //Console.ReadLine();
                return false;
            }
            logger.Debug("{0}, с/н: {1}, v{2}.{3}",
                ReaderTypeStrs[(int)rRdInf.nType],
                rRdInf.rBase.nSn,
                rRdInf.rBase.nVersion & 0xff, (rRdInf.rBase.nVersion >> 8) & 0xff);

            logger.Debug("Инициализация завершена");
            //Green();
            return true;
        }
        [STAThread]
        public void InitializaeZ2()// System.Windows.Forms.Label label)
        {
           
            //status = label;
            var rPorts = EnumSerialPorts();
            if (rPorts == null || rPorts.Count == 0)
            {
                logger.Fatal("Считыватель не найден...");
                //Red();
                return;
            }
            int hr;
            try
            {
                var data = string.Empty;
                if (lm.VerefecationLicense(ref data))
                    logger.Debug(data);
                hr = ZRIntf.ZR_Initialize(ZPIntf.ZP_IF_NO_MSG_LOOP);
                if (hr < 0)
                {
                    logger.Fatal("Ошибка ZR_Initialize ({0}).", hr);
                    //Red();
                    //Console.ReadLine();
                    return;
                }
            }
            catch (System.DllNotFoundException ex)
            {
                logger.Fatal($"{ex.Message}");
                //Red();
                return;
            }
            catch (Exception ex)
            {
                logger.Error($"{ex.Message}");
              
                return;
            }
            try
            {
                /////подключение к ком порту
                /////
                while (!Rd_Open(rPorts.First()))
                {
                    rPorts.RemoveAt(0);
                }

                int nCardCount = 0;
                hr = ZRIntf.ZR_Rd_SearchCards(m_hRd);
                if (hr < 0)
                {
                    logger.Fatal("Ошибка ZR_Rd_SearchCards ({0}).", hr);
                    //Console.ReadLine();
                    return;
                }
                ZR_CARD_INFO rInfo = new ZR_CARD_INFO();
                while ((hr = ZRIntf.ZR_Rd_FindNextCard(m_hRd, ref rInfo)) == ZRIntf.S_OK)
                {
                    logger.Debug("{0}. {1} {2}",
                        ++nCardCount, CardTypeStrs[(int)rInfo.nType], ZRIntf.CardNumToStr(rInfo.nNum, rInfo.nType));
                }
                if (hr < 0)
                {
                    logger.Debug("Ошибка ZR_Rd_FindNextCard ({0}).", hr);
                    // Console.ReadLine();
                    return;
                }
                ZRIntf.ZR_Rd_FindNextCard(m_hRd);
                //Console.WriteLine("--------------");
                //if (nCardCount > 0)
                //    Console.WriteLine("Найдено {0} карт.", nCardCount);
                //else
                //    Console.WriteLine("Карты не найдены.");

                m_oEvent = new ManualResetEvent(false);
                ZR_RD_NOTIFY_SETTINGS rNS = new ZR_RD_NOTIFY_SETTINGS(ZRIntf.ZR_RNF_EXIST_CARD, m_oEvent.SafeWaitHandle);
                hr = ZRIntf.ZR_Rd_SetNotification(m_hRd, rNS);
                if (hr < 0)
                {
                    logger.Fatal("Ошибка ZR_Rd_SetNotification ({0}).", hr);
                    //Red();
                    //Console.ReadLine();
                    return;
                }
                //StartNotifyTask();


            }
            catch (Exception)
            {
                StopNotifyTask();
                if (m_hRd != IntPtr.Zero)
                    ZRIntf.ZR_CloseHandle(m_hRd);
                ZRIntf.ZR_Finalyze();
                //Red();
            }
        }

    }
}
