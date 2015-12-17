using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.UI.Settings
{
    public class SettingsViewModel
    {
        [Range(1, int.MaxValue)]
        public int Port { get; set; }

        public string Message { get; set; }
    }
}