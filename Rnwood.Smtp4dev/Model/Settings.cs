using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Model
{
    [Serializable]
    public class Settings
    {
        public Settings()
        {
            Port = 25;
        }

        public int Port { get; set; }
    }
}