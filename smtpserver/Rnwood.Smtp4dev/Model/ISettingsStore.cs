using System;

namespace Rnwood.Smtp4dev.Model
{
    public interface ISettingsStore
    {
        Settings Load();

        void Save(Settings settings);

        event EventHandler Saved;
    }
}