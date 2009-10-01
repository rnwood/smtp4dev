using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;

namespace Rnwood.AutoUpdate
{
    public partial class UpdateAvailableForm : Form
    {
        public UpdateAvailableForm(Release release, Version currentVersion)
        {
            InitializeComponent();
            label1.Text = string.Format(label1.Text, "smtp4dev");
            Release = release;
            currVersion.Text = currentVersion.ToString();
            updateVersion.Text =  release.name + " (" + release.Version.ToString() + ")";
            detailsLink.Text = release.DetailsURL.ToString();
        }

        public Release Release { get; private set; }

        private void detailsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(Release.DetailsURL.ToString());
            }
            catch
            {
                MessageBox.Show(
                    "Failed to launch release details. Please check you have an active Internet connection and try again.",
                    "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }
    }
}
