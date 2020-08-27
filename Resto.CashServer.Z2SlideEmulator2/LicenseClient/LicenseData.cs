using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LicenseClient
{
	
    class LicenseData
    {
        public string Id { get; set; }
        public string Serial { get; set; }
        public string CompanyName { get; set; }
        public string Inception { get; set; }
        public string Expiration { get; set; }
        public string Signature { get; set; }
        public bool Online { get; set; }
    }
}
