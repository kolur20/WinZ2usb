using System.Management;

namespace LicenseClient
{
    class LicenseSystemInfo
    {
        

        public string MatheBoardID { get; private set; }
        public string ProcessorID { get; private set; }
        public string Key { get; private set; }
        public LicenseSystemInfo()
        {
            //StringBuilder systemInfo = new StringBuilder();

            ManagementClass manageClass1 = new ManagementClass("Win32_processor");
            ManagementObjectCollection manageObjects1 = manageClass1.GetInstances();
            PropertyDataCollection properties1 = manageClass1.Properties;
            foreach (ManagementObject obj in manageObjects1)
            {
                ProcessorID = obj.Properties["ProcessorId"].Value.ToString();
                //foreach (PropertyData property in properties1)
                //{
                //    try
                //    {
                //        systemInfo.AppendLine(property.Name + ":  " +
                //                        obj.Properties[property.Name].Value.ToString());
                //    }
                //    catch { }
                //}
                //systemInfo.AppendLine();
            }
            

            ManagementClass manageClass = new ManagementClass("Win32_OperatingSystem");
            ManagementObjectCollection manageObjects = manageClass.GetInstances();
            
            PropertyDataCollection properties = manageClass.Properties;
            foreach (ManagementObject obj in manageObjects)
            {
                MatheBoardID = obj.Properties["SerialNumber"].Value.ToString();
                //foreach (PropertyData property in properties)
                //{
                //    try
                //    {
                //        systemInfo.AppendLine(property.Name + ":  " +
                //                        obj.Properties[property.Name].Value.ToString());
                //    }
                //    catch { }
                //}
                //systemInfo.AppendLine();
            }
            //Console.WriteLine(systemInfo);

            Key = Coder.Encoding(MatheBoardID + ProcessorID);
            
        }

        public LicenseSystemInfo(string sn, string id)
        {
            MatheBoardID = sn;
            ProcessorID = id;
            Key = Coder.Encoding(sn + id);
        }
    }

         
  
}
