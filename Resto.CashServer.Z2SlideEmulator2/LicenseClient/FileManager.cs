using System;
using System.IO;

namespace LicenseClient
{
    internal class FileManager
    {
        string path;
        public FileManager()
        {
            path = Directory.GetCurrentDirectory() + "\\";
        }

        internal string[] GetLicenseFile(string fileNameLicense)
        {
            string[] str = new string[1];
            var i = 0;
            path += "Data\\" + fileNameLicense;
            if (File.Exists(path))
            {

                using (StreamReader sr = File.OpenText(path))
                {
                    string s;
                    while ((s = sr.ReadLine()) != null)
                    {
                        str[i++] += s;
                    }

                }

            }
            return str;
        }
    }
}