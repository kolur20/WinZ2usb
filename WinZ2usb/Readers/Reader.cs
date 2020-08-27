using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using ZREADER;
using ZPort;
using NLog;

namespace Reader
{
    static class Reader
    {
        static System.Windows.Forms.Label status = null;
        const ZP_PORT_TYPE RdPortType = ZP_PORT_TYPE.ZP_PORT_COM;
        //const string RdPortName = "COM3";

        static string[] ReaderTypeStrs = { "Неизвестно", "Z-2 USB", "Matrix III Rd-All", "Z-2 USB MF", "Matrix III Net", "CP-Z-2MF", "Z-2 EHR", "Z-2 Base", "Matrix V" };
        static string[] CardTypeStrs = { "Неизвестно", "EM", "HID", "IC", "UL", "1K", "4K", "DF", "PX",
                                         "Cod433 Fix", "Cod433", "Dallas", "CAME", "Plus", "Plus 1K", "Plus 2K", "Plus 4K", "Mini" };

        public static IntPtr m_hRd;
        public static readonly Byte[] MfAuthKey = new Byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        static Logger logger = LogManager.GetCurrentClassLogger();
        static ManualResetEvent m_oEvent = null;
        static bool m_fThreadActive;
        static Thread m_oThread = null;
        #region markers
        static private void Yellow() { status.BackColor = Color.Yellow; }
        static private void Green() { status.BackColor = Color.LightGreen; }
        static private void Red() { status.BackColor = Color.Red; }
        #endregion
        static public void LoggerMessage(string msg)
        {
            logger.Debug(msg);
        }
        //считывание данных из блоков
        static void DoRead1K4K(Byte[] rNum, ZR_CARD_TYPE nCdType)
        {
            int startTick;
            ZRIntf.ZR_Rd_SetCapture(m_hRd);
            try
            {
                int nRet;
                nRet = ZRIntf.ZR_Rd_SelectCard(m_hRd, rNum, nCdType);
                if (nRet < 0)
                {
                    logger.Error("Ошибка ZR_Rd_SelectCard ({0:X4}).", nRet);
                    Red();
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
                            logger.Fatal("Ошибка ZR_Rd_AuthorizeSect ({0:X4}).", nRet);
                            Red();
                            //Console.ReadLine();
                            return;
                        }
                        //Console.WriteLine("-----------------------");
                        logger.Info("Сектор {0}", nSectN);
                    }
                    nRet = ZRIntf.ZR_Rd_ReadMfCardBlock(m_hRd, i, aBuf, 0);
                    if (nRet < 0)
                    {
                        if (nRet == ZRIntf.ZR_E_CARDNACK)
                        {
                            logger.Error("\t({0}-{1}): Отказ карты.", nSectN, nSBlockN);
                            i++;
                            continue;
                        }
                        logger.Fatal("Ошибка ZR_Rd_ReadMfCardBlock ({0:X4}).", nRet);
                        Red();
                        //Console.ReadLine();
                        return;
                    }
                    logger.Info("{0} ({1}-{2}): {3:X2} {4:X2} {5:X2} {6:X2} " +
                        "{7:X2} {8:X2} {9:X2} {10:X2} {11:X2} {12:X2} {13:X2} " +
                        "{14:X2} {15:X2} {16:X2} {17:X2} {18:X2}",
                        i, nSectN, nSBlockN,
                        aBuf[0], aBuf[1], aBuf[2], aBuf[3],
                        aBuf[4], aBuf[5], aBuf[6], aBuf[7],
                        aBuf[8], aBuf[9], aBuf[10], aBuf[11],
                        aBuf[12], aBuf[13], aBuf[14], aBuf[15]);
                    var keyStr = "";
                    foreach (var k in aBuf)
                    {
                        keyStr += (k - '0').ToString();
                        KeyboardSend.KeyDown((char)(k));
                    }
                    KeyboardSend.KeyDown(System.Windows.Forms.Keys.Enter);
                    logger.Info($"Трек карты: {keyStr}");

                    if (nSBlockN == (nSBlockMax - 1))
                    {
                        Byte[] aAccBytes = new Byte[4];
                        Array.Copy(aBuf, 6, aAccBytes, 0, 4);
                    }
                    i++;
                }
                logger.Info("Успешно. ");
            }
            finally
            {
                ZRIntf.ZR_Rd_ReleaseCapture(m_hRd);
            }
            TimeSpan rezultTime = TimeSpan.FromMilliseconds(Environment.TickCount - startTick);
            logger.Info("Общее время: {0}", rezultTime);
        }
       static int CheckNotifyMsgs()
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
                            logger.Info("Карта поднесена {0} {1}",
                               CardTypeStrs[(int)pInfo.nType],
                               ZRIntf.CardNumToStr(pInfo.nNum, pInfo.nType));
                            switch (pInfo.nType)
                            {
                                case ZR_CARD_TYPE.ZR_CD_UL:
                                    //DoReadUL(pInfo.nNum, pInfo.nType);
                                    break;
                                case ZR_CARD_TYPE.ZR_CD_1K:
                                case ZR_CARD_TYPE.ZR_CD_4K:
                                    DoRead1K4K(pInfo.nNum, pInfo.nType);
                                    break;
                                default:
                                   logger.Error("Не могу прочитать карту.");
                                    break;
                            }
                        }
                        break;
                    case ZRIntf.ZR_RN_CARD_REMOVE:
                        {
                            ZR_CARD_INFO pInfo = (ZR_CARD_INFO)Marshal.PtrToStructure(nMsgParam, typeof(ZR_CARD_INFO));
                           logger.Info("Удалена карта {0} {1}",
                                CardTypeStrs[(int)pInfo.nType],
                                ZRIntf.CardNumToStr(pInfo.nNum, pInfo.nType));
                        }
                        break;
                }
            }
            if (hr == ZPIntf.ZP_S_NOTFOUND)
                hr = ZRIntf.S_OK;
            return hr;
        }

        

        static void DoNotifyWork()
        {
            while (m_fThreadActive)
            {
                if (m_oEvent.WaitOne())
                {
                    m_oEvent.Reset();
                    if (m_hRd != IntPtr.Zero)
                        CheckNotifyMsgs();
                }
            }
        }

        static void StartNotifyThread()
        {
            if (m_oThread != null)
                return;
            m_fThreadActive = true;
            m_oThread = new Thread(DoNotifyWork);
            m_oThread.Start();
        }
        static void StopNotifyThread()
        {
            if (m_oThread == null)
                return;
            m_fThreadActive = false;
            m_oEvent.Set();
            m_oThread.Join();
            m_oThread = null;
        }
        static public void Dispose()
        {
            try
            {
                StopNotifyThread();
                if (m_hRd != IntPtr.Zero)
                    ZRIntf.ZR_CloseHandle(m_hRd);
                ZRIntf.ZR_Finalyze();
            }
            catch (Exception e)
            {
                logger.Fatal(e.Message);
            }
            
        }
        static List<string> EnumSerialPorts()
        {
            List<string> rPorts = null;
            try
            {
               
                UInt32 nVersion = ZRIntf.ZR_GetVersion();
                UInt32 nVerMajor = (nVersion & 0xFF);
                UInt32 nVerMinor = ((nVersion >> 8) & 0xFF);
                UInt32 nVerBuild = ((nVersion >> 16) & 0xFF);
                logger.Info("SDK Reader v{0}.{1}.{2}", nVerMajor, nVerMinor, nVerBuild);
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
            
                logger.Info("Перечисление последовательных портов...");
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
                        logger.Info("{0}. {1} ({2}); {3}",
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
                    logger.Info("Найдено {0} портов.", nPortCount);
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
                StopNotifyThread();
                if (m_hRd != IntPtr.Zero)
                    ZRIntf.ZR_CloseHandle(m_hRd);
                //ZRIntf.ZR_Finalyze();
                Red();
            }
        }
        static bool Rd_Open(string PortName)
        {
            int hr;
            ZR_RD_INFO rRdInf = new ZR_RD_INFO();
            logger.Info("Подключение к считывателю ({0})...", PortName);
            ZR_RD_OPEN_PARAMS rOpen = new ZR_RD_OPEN_PARAMS(RdPortType, PortName);
            hr = ZRIntf.ZR_Rd_Open(ref m_hRd, ref rOpen, ref rRdInf);
            if (hr < 0)
            {
                logger.Fatal("Ошибка ZR_Rd_Open ({0}).", hr);
                Red();
                //Console.ReadLine();
                return false;
            }
            logger.Info("{0}, с/н: {1}, v{2}.{3}",
                ReaderTypeStrs[(int)rRdInf.nType],
                rRdInf.rBase.nSn,
                rRdInf.rBase.nVersion & 0xff, (rRdInf.rBase.nVersion >> 8) & 0xff);

            logger.Info("Инициализация завершена");
            Green();
            return true;
        }
        static public  void InitializaeZ2(System.Windows.Forms.Label label)
        {
            status = label;
            var rPorts = EnumSerialPorts();
            if (rPorts == null || rPorts.Count == 0)
            {
                //logger.Fatal("Считыватель не найден...");
                Red();
                return;
            }
            int hr;
            try
            {
                hr = ZRIntf.ZR_Initialize(ZPIntf.ZP_IF_NO_MSG_LOOP);
                if (hr < 0)
                {
                    logger.Fatal("Ошибка ZR_Initialize ({0}).", hr);
                    Red();
                    //Console.ReadLine();
                    return;
                }
            }
            catch (System.DllNotFoundException ex)
            {
                logger.Fatal($"{ex.Message}");
                Red();
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
                    logger.Info("{0}. {1} {2}",
                        ++nCardCount, CardTypeStrs[(int)rInfo.nType], ZRIntf.CardNumToStr(rInfo.nNum, rInfo.nType));
                }
                if (hr < 0)
                {
                    logger.Fatal("Ошибка ZR_Rd_FindNextCard ({0}).", hr);
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
                    Red();
                    //Console.ReadLine();
                    return;
                }
                StartNotifyThread();
                //Console.WriteLine();
                //Console.WriteLine("Ожидание поднесения карт...");
                //Console.WriteLine();
                //Console.ReadLine();
            }
            catch (Exception)
            {
                StopNotifyThread();
                if (m_hRd != IntPtr.Zero)
                    ZRIntf.ZR_CloseHandle(m_hRd);
                ZRIntf.ZR_Finalyze();
                Red();
            }
        }

    }
}
