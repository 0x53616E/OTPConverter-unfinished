//LicenseData.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BUConverter.License
{
    public static class LicenseData
    {
        // Lassen Sie die HWID bestehen, falls Sie sie intern noch benötigen
        public static readonly string AuthorizedHWID = "".Trim();

        // NEU: Deklaration für den Namen der lizenzierten Person
        public static readonly string LicenseeName = "".Trim(); 
    }
}