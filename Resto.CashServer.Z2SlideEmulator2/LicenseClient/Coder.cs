using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LicenseClient
{
    class Coder
    {
        public static string Encoding(string data)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] dataC = System.Text.Encoding.UTF8.GetBytes(data);
            byte[] hash = md5.ComputeHash(dataC);
            return Convert.ToBase64String(hash).TrimEnd('=');

        }
        
    }
}
