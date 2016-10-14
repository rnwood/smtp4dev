using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Model
{
    public class Settings
    {
        public Settings()
        {
            Port = 25;
            IsEnabled = true;
        }

        public bool IsEnabled { get; set; }
        public int Port { get; set; }
    }
}