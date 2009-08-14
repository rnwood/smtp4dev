using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace Rnwood.Smtp4dev
{
    public class MIMEDatabase
    {
        public static string GetExtension(string mimeType)
        {
            mimeType = mimeType.ToLower();

            RegistryKey key = Registry.ClassesRoot.OpenSubKey("MIME\\Database\\Content Type", false);
            
            if (key.GetSubKeyNames().Contains(mimeType))
            {
                return (string) key.OpenSubKey(mimeType, false).GetValue("Extension");
            }

            return null;
            
        }
    }
}
