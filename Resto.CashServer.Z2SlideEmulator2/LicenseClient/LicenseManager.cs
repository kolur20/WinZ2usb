using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LicenseClient
{
    class LicenseManager
    {
        string FileNameLicense = "LicensingState.lic";
        LicenseData licenseData;
        string GetCurrentDateTime()
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz)
                    .ToString(CultureInfo.GetCultureInfo("ru-ru"));
        }
        public bool VerefecationLicenseDate()
        {
            var now = Convert.ToDateTime(GetCurrentDateTime());
            if (Convert.ToDateTime(licenseData.Inception) < now &&
               Convert.ToDateTime(licenseData.Expiration) > now)
                return true;
            else
                throw new Exception("Истек срок действия лицензии");
        }
        public bool VerefecationLicense()
        {
            try
            {
                var fm = new FileManager();
                string[] lic = fm.GetLicenseFile(FileNameLicense);
                if (lic[0] == null ||lic[0].Count() == 0)
                    throw new Exception("Файл лицензии не найден");
                licenseData = GetLicense(lic[0]);
                LicenseSystemInfo systemInfo = new LicenseSystemInfo();
                var verSignature = Coder.Encoding(licenseData.Id +
                    licenseData.Serial +
                    licenseData.Inception +
                    licenseData.Expiration +
                    licenseData.Online +
                    systemInfo.Key);

                if (licenseData.Signature == verSignature &&
                        licenseData.Id == systemInfo.ProcessorID &&
                        licenseData.Serial == systemInfo.MatheBoardID)
                {
                    return VerefecationLicenseDate() ? true :
                        throw new Exception("Истек срок действия лицензии");
                }
                else throw new Exception("Ваша копия программы не лицензирована!");
            }
            catch (IndexOutOfRangeException)
            {
                throw new Exception("Файл лицензии поврежден");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
                
            }
        }

        private LicenseData GetLicense(string lic)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(lic);
            var license = new LicenseData();
            license.Id = doc.ChildNodes[0].SelectSingleNode(@"/license/Id", null).InnerText;
            license.Serial = doc.ChildNodes[0].SelectSingleNode(@"/license/Serial", null).InnerText;
            license.CompanyName = doc.ChildNodes[0].SelectSingleNode(@"/license/CompanyName", null).InnerText;
            license.Inception = doc.ChildNodes[0].SelectSingleNode(@"/license/Inception", null).InnerText;
            license.Expiration = doc.ChildNodes[0].SelectSingleNode(@"/license/Expiration", null).InnerText;
            license.Online = Convert.ToBoolean(doc.ChildNodes[0].SelectSingleNode(@"/license/Online", null).InnerText);
            license.Signature = doc.ChildNodes[0].SelectSingleNode(@"/license/Signature", null).InnerText;
            return license;
        }
    }
}
