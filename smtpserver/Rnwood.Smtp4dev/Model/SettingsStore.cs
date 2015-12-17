using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Model
{
    public class SettingsStore : ISettingsStore
    {
        private Settings _settings = new Settings();

        public Settings Load()
        {
            return _settings;
        }

        public void Save(Settings settings)
        {
            _settings = settings;
        }
    }
}