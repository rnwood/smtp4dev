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
using anmar.SharpMimeTools;
using Rnwood.SmtpServer;
using Rnwood.Smtp4dev.MessageInspector;

namespace Rnwood.Smtp4dev
{
    public partial class MainForm : Form
    {
        public MainForm(LaunchInfo launchInfo)
        {
            InitializeComponent();

            _launchInfo = launchInfo;

            messageBindingSource.DataSource = _messages;
            sessionBindingSource.DataSource = _sessions;
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
                ProcessLaunchInfo(_launchInfo, true);
            }
        }

        void ServerWork()
        {
            try
            {
                Application.DoEvents();

                Smtp4DevServerBehaviour b = new Smtp4DevServerBehaviour();
                b.MessageReceived += MessageReceived;
                b.SessionCompleted += new EventHandler<SessionCompletedEventArgs>(b_SessionCompleted);

                _server = new Server(b);
                _server.Run();
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

        void b_SessionCompleted(object sender, SessionCompletedEventArgs e)
        {
            Invoke((MethodInvoker)(() =>
                                            {
                                                _sessions.Add(new SessionViewModel(e.Session));
                                            }));
        }

        private void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            MessageViewModel message = new MessageViewModel(e.Message);

            Invoke((MethodInvoker)(() =>
            {
                _messages.Add(message);

                if (Properties.Settings.Default.MaxMessages > 0)
                {
                    while (_messages.Count > Properties.Settings.Default.MaxMessages)
                    {
                        _messages.RemoveAt(0);
                    }
                }

                if (Properties.Settings.Default.AutoViewNewMessages || Properties.Settings.Default.AutoInspectNewMessages)
                {
                    if (Properties.Settings.Default.AutoViewNewMessages)
                    {
                        ViewMessage(message);
                    }

                    if (Properties.Settings.Default.AutoInspectNewMessages)
                    {
                        InspectMessage(message);
                    }
                }
                else if (!Visible && Properties.Settings.Default.BalloonNotifications)
                {
                    string body = string.Format("From: {0}\nTo: {1}\nSubject: {2}\n<Click here to view more details>",
                        message.From,
                        message.To,
                        message.Subject);

                    trayIcon.ShowBalloonTip(3000, "Message Recieved", body, ToolTipIcon.Info);
                }

                if (Visible && Properties.Settings.Default.BringToFrontOnNewMessage)
                {
                    BringToFront();
                    Activate();
                }
            }));
        }

        private Server _server;

        private readonly BindingList<MessageViewModel> _messages = new BindingList<MessageViewModel>();
        private readonly BindingList<SessionViewModel> _sessions = new BindingList<SessionViewModel>();
        private LaunchInfo _launchInfo;


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Quit();
        }


        private void trayIcon_DoubleClick(object sender, EventArgs e)
        {
            Visible = true;
        }

        public MessageViewModel SelectedMessage
        {
            get
            {
                if (messageGrid.SelectedRows.Count != 1)
                {
                    return null;
                }

                return
                    messageGrid.SelectedRows.Cast<DataGridViewRow>().Select(row => (MessageViewModel)row.DataBoundItem).Single();
            }
        }


        public MessageViewModel[] SelectedMessages
        {
            get
            {
                return
                    messageGrid.SelectedRows.Cast<DataGridViewRow>().Select(row => (MessageViewModel)row.DataBoundItem).ToArray();
            }
        }

        public SessionViewModel SelectedSession
        {
            get
            {
                return
                    (SessionViewModel)sessionsGrid.SelectedRows.Cast<DataGridViewRow>().Select(row => row.DataBoundItem).FirstOrDefault();
            }
        }

        private void viewButton_Click(object sender, EventArgs e)
        {
            ViewSelectedMessages();
        }

        private void ViewSelectedMessages()
        {
            foreach (MessageViewModel message in SelectedMessages)
            {
                ViewMessage(message);
            }
        }

        private void ViewMessage(MessageViewModel message)
        {
            TempFileCollection tempFiles = new TempFileCollection();
            FileInfo msgFile = new FileInfo(tempFiles.AddExtension("eml"));
            message.SaveToFile(msgFile);

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
            ViewSelectedMessages();
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
            if (_server.IsRunning)
            {
                StopServer();
            }
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
            if (new OptionsForm().ShowDialog() == DialogResult.OK)
            {
                if (_server.IsRunning)
                {
                    StopServer();
                    StartServer();
                }
            }
        }

        private void trayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if (_messages.Count > 0)
            {
                if (Properties.Settings.Default.InspectOnBalloonClick)
                {
                    InspectMessage(_messages.Last());
                }
                else
                {
                    ViewMessage(_messages.Last());
                }
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
            foreach (MessageViewModel message in SelectedMessages)
            {
                if (saveMessageFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        message.SaveToFile(new FileInfo(saveMessageFileDialog.FileName));
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
            foreach (MessageViewModel message in SelectedMessages)
            {
                _messages.Remove(message);
            }
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
            inspectMessageButton.Enabled = deleteButton.Enabled = viewButton.Enabled = saveButton.Enabled = SelectedMessages.Length > 0;
        }

        private void messageGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                MessageViewModel message = (MessageViewModel)messageGrid.Rows[e.RowIndex].DataBoundItem;

                if (!message.HasBeenViewed)
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

        private void inspectButton_Click(object sender, EventArgs e)
        {
            foreach (MessageViewModel message in SelectedMessages)
            {
                InspectMessage(message);
            }
        }

        private void InspectMessage(MessageViewModel message)
        {
            message.MarkAsViewed();

            InspectorWindow form = new InspectorWindow(message.Message.Contents);
            form.Show();

            messageGrid.Refresh();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            SelectedSession.ViewLog();
        }

        private void sessionsGrid_SelectionChanged(object sender, EventArgs e)
        {
            viewSessionButton.Enabled = SelectedSession != null;
        }

        public void ProcessLaunchInfo(LaunchInfo launchInfo, bool firstInstance)
        {
            if (firstInstance)
            {
                Visible = true;
                Visible = !Properties.Settings.Default.StartInTray;

                if (Properties.Settings.Default.ListenOnStartup)
                {
                    StartServer();
                }
            }

            if (launchInfo.Arguments.Length == 1)
            {
                string messageFilename = launchInfo.Arguments[0];
                if (File.Exists(messageFilename))
                {
                    SharpMimeMessage message = new SharpMimeMessage(File.OpenRead(messageFilename));
                    InspectorWindow messageInspector = new InspectorWindow(message);
                    messageInspector.Show();
                }
                else
                {
                    throw new Exception("Specified file does not exist");
                }
            }
            else if (launchInfo.Arguments.Length == 0)
            {
                if (!firstInstance)
                {
                    Visible = true;
                }
            }
            else
            {
                throw new Exception("Invalid command line parameters");
            }
        }
    }
}
