using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace smtp4dev
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(true);

            Properties.Settings.Default.Upgrade();

            MainForm form = new MainForm();
            Application.Run(form);
        }
    }
}
