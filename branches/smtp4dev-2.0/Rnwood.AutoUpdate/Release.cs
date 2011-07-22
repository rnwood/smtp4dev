using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Rnwood.AutoUpdate
{
    public partial class Release
    {
        public void InitiateDownload()
        {
            Process.Start(URL.ToString());
        }

        public Version Version
        {
            get
            {
                return new Version(version);
            }
        }
    }
}
