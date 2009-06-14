using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using EricDaugherty.CSES.Common;
using EricDaugherty.CSES.Net;
using EricDaugherty.CSES.SmtpServer;
using smtp4dev.Properties;

namespace smtp4dev
{
    public partial class MainForm : Form, IMessageSpool, IRecipientFilter
    {
        public MainForm()
        {
            InitializeComponent();

            bindingSource.DataSource = _messages;
            _messages.ListChanged += _messages_ListChanged;

            Icon = Properties.Resources.Icon1;
            trayIcon.Icon = Properties.Resources.Icon2;

        }

        private void StartServer()
        {
            trayIcon.Icon = Properties.Resources.Icon1;
            listenForConnectionsToolStripMenuItem.Checked = true;
            statusLabel.Text = "Listening";
            pictureBox2.Visible = stopListeningButton.Visible = true;
            pictureBox3.Visible = startListeningButton.Visible = false;

            new Thread(ServerWork).Start();
        }

        void _messages_ListChanged(object sender, ListChangedEventArgs e)
        {
            deleteAllMenuItem.Enabled = deleteAllButton.Enabled = viewLastMessageMenuItem.Enabled = _messages.Count > 0;
            trayIcon.Text = string.Format("smtp4dev ({0} messages)", _messages.Count);

            if (e.ListChangedType == ListChangedType.ItemAdded && Properties.Settings.Default.ScrollMessages && messageGrid.RowCount > 0)
            {
                messageGrid.ClearSelection();
                messageGrid.Rows[messageGrid.RowCount - 1].Selected = true;
                messageGrid.FirstDisplayedScrollingRowIndex = messageGrid.RowCount - 1;
            }
        }

        private bool _firstTimeShown = true;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (_firstTimeShown)
            {
                _firstTimeShown = false;

                Visible = true;
                Visible = !Properties.Settings.Default.StartInTray;

                if (Properties.Settings.Default.ListenOnStartup)
                {
                    StartServer();
                }
            }
        }

        void ServerWork()
        {
            try
            {
                Application.DoEvents();

                SMTPProcessor processor = new SMTPProcessor("localhost", this, this)
                                              {
                                                  WelcomeMessage = "220 localhost smtp4dev"
                                              };
                _server = new SimpleServer(Settings.Default.PortNumber, processor.ProcessConnection);
                _server.Start();
            }
            catch (Exception exception)
            {
                Invoke((MethodInvoker)(() =>
                                            {
                                                StopServer();
                                                statusLabel.Text = "Server failed: " + exception.Message;

                                                trayIcon.ShowBalloonTip(3000, "Server failed", exception.Message, ToolTipIcon.Error);
                                            }));
            }
        }

        private SimpleServer _server;

        bool IMessageSpool.SpoolMessage(SMTPMessage message)
        {
            Email email = new Email(message);

            Invoke((MethodInvoker)(() =>
                                       {
                                           _messages.Add(email);

                                           if (Properties.Settings.Default.MaxMessages > 0)
                                           {
                                               while (_messages.Count > Properties.Settings.Default.MaxMessages)
                                               {
                                                   _messages.RemoveAt(0);
                                               }
                                           }                                          

                                           if (Properties.Settings.Default.AutoViewNewMessages)
                                           {
                                               ViewMessage(email);
                                           }
                                           else if (!Visible && Properties.Settings.Default.BalloonNotifications)
                                           {
                                               string body = string.Format("From: {0}\nTo: {1}\nSubject: {2}\n<Click here to view more details>",
                                                   email.FromAddress,
                                                   string.Join(", ", email.ToAddresses),
                                                   email.Subject);

                                               trayIcon.ShowBalloonTip(3000, "Email Recieved", body, ToolTipIcon.Info);
                                           }

                                           if (Visible && Properties.Settings.Default.BringToFrontOnNewMessage)
                                           {
                                               BringToFront();
                                               Activate();
                                           }
                                       }));

            return true;
        }

        private readonly BindingList<Email> _messages = new BindingList<Email>();

        bool IRecipientFilter.AcceptRecipient(SMTPContext context, EmailAddress recipient)
        {
            return true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Quit();
        }


        private void trayIcon_DoubleClick(object sender, EventArgs e)
        {
            Visible = true;
        }

        public Email SelectedEmail
        {
            get
            {
                return
                    (Email)messageGrid.SelectedRows.Cast<DataGridViewRow>().Select(row => row.DataBoundItem).FirstOrDefault();
            }
        }

        private void viewButton_Click(object sender, EventArgs e)
        {
            ViewSelectedMessage();
        }

        private void ViewSelectedMessage()
        {
            if (SelectedEmail != null)
            {
                ViewMessage(SelectedEmail);
            }
        }

        private void ViewMessage(Email email)
        {
            TempFileCollection tempFiles = new TempFileCollection();
            FileInfo msgFile = new FileInfo(tempFiles.AddExtension("eml"));
            email.SaveToFile(msgFile);

            Process.Start(msgFile.FullName);
            messageGrid.Refresh();
        }

        private void deleteAllButton_Click(object sender, EventArgs e)
        {
            DeleteAllMessages();
        }

        private void DeleteAllMessages()
        {
            _messages.Clear();
        }

        private void messageGrid_DoubleClick(object sender, EventArgs e)
        {
            ViewSelectedMessage();
        }

        private void clearAllEmailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteAllMessages();
        }

        private void MainForm_VisibleChanged(object sender, EventArgs e)
        {
            trayIcon.Visible = !Visible;

            if (Visible)
            {
                WindowState = FormWindowState.Normal;
                Activate();
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Quit();
        }

        private void Quit()
        {
            StopServer();
            Application.Exit();
        }

        private void StopServer()
        {
            trayIcon.Icon = Properties.Resources.Icon2;
            listenForConnectionsToolStripMenuItem.Checked = false;
            statusLabel.Text = "Not listening";
            pictureBox2.Visible = stopListeningButton.Visible = false;
            pictureBox3.Visible = startListeningButton.Visible = true;
            _server.Stop();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ViewMessage(_messages.Last());
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditOptions();
        }

        private void EditOptions()
        {
            new OptionsForm().ShowDialog();
        }

        private void trayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if (_messages.Count > 0)
            {
                ViewMessage(_messages.Last());
            }
            else
            {
                Visible = true;
            }
        }

        private void optionsButton_Click(object sender, EventArgs e)
        {
            EditOptions();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (SelectedEmail != null)
            {
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        SelectedEmail.SaveToFile(new FileInfo(saveFileDialog.FileName));
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show(string.Format("Failed to save: {0}", ex.Message), "Error", MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                    }


                }
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            _messages.Remove(SelectedEmail);
        }

        private void stopListeningButton_Click(object sender, EventArgs e)
        {
            StopServer();
        }

        private void startListeningButton_Click(object sender, EventArgs e)
        {
            StartServer();
        }


        private void listenForConnectionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listenForConnectionsToolStripMenuItem.Checked)
            {
                StopServer();
            }
            else
            {
                StartServer();
            }
        }

        private void messageGrid_SelectionChanged(object sender, EventArgs e)
        {
            button1.Enabled = deleteButton.Enabled = viewButton.Enabled = saveButton.Enabled = SelectedEmail != null;
        }

        private void messageGrid_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            Email email = (Email)messageGrid.Rows[e.RowIndex].DataBoundItem;

            if (messageGrid.Columns[e.ColumnIndex] == ToAddressesNice)
            {
                e.Value = string.Join(", ", email.ToAddresses);
            }
        }

        private void messageGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                Email email = (Email)messageGrid.Rows[e.RowIndex].DataBoundItem;

                if (!email.HasBeenViewed)
                {
                    e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Bold);
                }
            }
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Visible = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Email email = SelectedEmail;
            TempFileCollection tempFiles = new TempFileCollection();
            FileInfo msgFile = new FileInfo(tempFiles.AddExtension("txt"));
            email.SaveToFile(msgFile);

            Process.Start(msgFile.FullName);
        }

    }
}
