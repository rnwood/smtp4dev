#region

using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Forms;
using Rnwood.Smtp4dev.Properties;

#endregion

namespace Rnwood.Smtp4dev
{
    public partial class OptionsForm : Form
    {
        public OptionsForm()
        {
            InitializeComponent();

            Icon = Resources.ListeningIcon;
            checkBox3.Checked = RegistrySettings.StartOnLogin;

            UpdateControlStatus();

            ipAddressCombo.DataSource =
                new[] {IPAddress.Any}.Concat(
                    NetworkInterface.GetAllNetworkInterfaces().SelectMany(ni => ni.GetIPProperties().UnicastAddresses).
                        Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork).Select(ua => ua.Address)).
                    ToList();
            ipAddressCombo.SelectedItem = IPAddress.Parse(Settings.Default.IPAddress);
        }


        private void button2_Click(object sender, EventArgs e)
        {
            RegistrySettings.StartOnLogin = checkBox3.Checked;
            Settings.Default.IPAddress = (ipAddressCombo.SelectedItem).ToString();
            Settings.Default.Save();
            DialogResult = DialogResult.OK;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Settings.Default.Reload();
            DialogResult = DialogResult.Cancel;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        private void OptionsForm_Load(object sender, EventArgs e)
        {
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControlStatus();
        }

        private void UpdateControlStatus()
        {
            checkBox2.Enabled = !checkBox4.Checked && checkBox6.Checked;
            checkBox1.Enabled = checkBox6.Checked;
            checkBox7.Enabled = !checkBox4.Checked;
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControlStatus();

            if (!checkBox6.Checked)
            {
                checkBox1.Checked = false;
            }

            if (checkBox4.Checked)
            {
                checkBox7.Checked = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (openSSLCertDialog.ShowDialog() == DialogResult.OK)
            {
                Settings.Default.SSLCertificatePath = openSSLCertDialog.FileName;
            }
        }

        private void checkForUpdateButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Program.CheckForUpdateCore())
                {
                    MessageBox.Show("No update available", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error checking for update.\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}