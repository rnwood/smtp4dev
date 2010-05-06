#region

using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using anmar.SharpMimeTools;
using Microsoft.Win32;
using Rnwood.Smtp4dev.MessageInspector;
using Rnwood.Smtp4dev.Properties;
using Rnwood.SmtpServer;
using Message = Rnwood.SmtpServer.Message;

#endregion

namespace Rnwood.Smtp4dev
{
    public partial class MainForm : Form
    {
        private readonly BindingList<MessageViewModel> _messages = new BindingList<MessageViewModel>();
        private readonly BindingList<SessionViewModel> _sessions = new BindingList<SessionViewModel>();
        private Server _server;
        private bool _quitting;

        public MainForm()
        {
            InitializeComponent();

            messageBindingSource.DataSource = _messages;
            sessionBindingSource.DataSource = _sessions;
            _messages.ListChanged += _messages_ListChanged;

            Icon = Resources.ListeningIcon;
            trayIcon.Icon = Resources.NotListeningIcon;
        }

        private bool _firstShown = true;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (_firstShown)
            {

                Visible = true;
                Visible = !Settings.Default.StartInTray;

                if (Settings.Default.ListenOnStartup)
                {
                    StartServer();
                }
            }

            _firstShown = false;
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
                    messageGrid.SelectedRows.Cast<DataGridViewRow>().Select(row => (MessageViewModel)row.DataBoundItem)
                        .Single();
            }
        }


        public MessageViewModel[] SelectedMessages
        {
            get
            {
                return
                    messageGrid.SelectedRows.Cast<DataGridViewRow>().Select(row => (MessageViewModel)row.DataBoundItem)
                        .ToArray();
            }
        }

        public SessionViewModel SelectedSession
        {
            get
            {
                return
                    (SessionViewModel)
                    sessionsGrid.SelectedRows.Cast<DataGridViewRow>().Select(row => row.DataBoundItem).FirstOrDefault();
            }
        }

        public SessionViewModel[] SelectedSessions
        {
            get
            {
                return
                    sessionsGrid.SelectedRows.Cast<DataGridViewRow>().Select(row => (SessionViewModel)row.DataBoundItem)
                        .ToArray();
            }
        }

        private void StartServer()
        {
            new Thread(ServerWork).Start();

            trayIcon.Text = string.Format("smtp4dev (listening on :{0})\n{1} messages", Settings.Default.PortNumber, _messages.Count);
            trayIcon.Icon = Resources.ListeningIcon;
            listenForConnectionsToolStripMenuItem.Checked = true;
            statusLabel.Text = string.Format("Listening on port {0}", Settings.Default.PortNumber);
            runningPicture.Visible = stopListeningButton.Visible = true;
            notRunningPicture.Visible = startListeningButton.Visible = false;
        }

        private void _messages_ListChanged(object sender, ListChangedEventArgs e)
        {
            deleteAllMenuItem.Enabled = deleteAllButton.Enabled = viewLastMessageMenuItem.Enabled = _messages.Count > 0;
            trayIcon.Text = string.Format("smtp4dev (listening on :{0})\n{1} messages", Settings.Default.PortNumber, _messages.Count);

            if (e.ListChangedType == ListChangedType.ItemAdded && Settings.Default.ScrollMessages &&
                messageGrid.RowCount > 0)
            {
                messageGrid.ClearSelection();
                messageGrid.Rows[messageGrid.RowCount - 1].Selected = true;
                messageGrid.FirstDisplayedScrollingRowIndex = messageGrid.RowCount - 1;
            }
        }

        private void ServerWork()
        {
            try
            {
                Application.DoEvents();

                ServerBehaviour b = new ServerBehaviour();
                b.MessageReceived += OnMessageReceived;
                b.SessionCompleted += OnSessionCompleted;

                _server = new Server(b);
                _server.Run();
            }
            catch (Exception exception)
            {
                Invoke((MethodInvoker)(() =>
                                            {

                                                StopServer();

                                                statusLabel.Text = "Server failed: " + exception.Message;

                                                trayIcon.ShowBalloonTip(3000, "Server failed", exception.Message,
                                                                        ToolTipIcon.Error);
                                            }));
            }
        }

        private void OnSessionCompleted(object sender, SessionEventArgs e)
        {
            Invoke((MethodInvoker)(() => { _sessions.Add(new SessionViewModel(e.Session)); }));
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            MessageViewModel message = new MessageViewModel(e.Message);

            Invoke((MethodInvoker)(() =>
                                        {
                                            _messages.Add(message);

                                            if (Settings.Default.MaxMessages > 0)
                                            {
                                                while (_messages.Count > Settings.Default.MaxMessages)
                                                {
                                                    _messages.RemoveAt(0);
                                                }
                                            }

                                            if (Settings.Default.AutoViewNewMessages ||
                                                Settings.Default.AutoInspectNewMessages)
                                            {
                                                if (Settings.Default.AutoViewNewMessages)
                                                {
                                                    ViewMessage(message);
                                                }

                                                if (Settings.Default.AutoInspectNewMessages)
                                                {
                                                    InspectMessage(message);
                                                }
                                            }
                                            else if (!Visible && Settings.Default.BalloonNotifications)
                                            {
                                                string body =
                                                    string.Format(
                                                        "From: {0}\nTo: {1}\nSubject: {2}\n<Click here to view more details>",
                                                        message.From,
                                                        message.To,
                                                        message.Subject);

                                                trayIcon.ShowBalloonTip(3000, "Message Recieved", body, ToolTipIcon.Info);
                                            }

                                            if (Visible && Settings.Default.BringToFrontOnNewMessage)
                                            {
                                                BringToFront();
                                                Activate();
                                            }
                                        }));
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Quit();
        }


        private void trayIcon_DoubleClick(object sender, EventArgs e)
        {
            Visible = true;
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

            if (Registry.ClassesRoot.OpenSubKey(".eml", false) == null || string.IsNullOrEmpty((string)Registry.ClassesRoot.OpenSubKey(".eml", false).GetValue(null)))
            {
                switch (MessageBox.Show(this,
                                        "You don't appear to have a viewer application associated with .eml files!\nWould you like to download Windows Live Mail (free from live.com website)?",
                                        "View Message", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Yes:
                        Process.Start("http://download.live.com/wlmail");
                        return;
                        break;
                    case DialogResult.Cancel:
                        return;
                        break;
                }
            }

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
            _sessions.Clear();
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
            trayIcon.Visible = false;
            _quitting = true;
            Application.Exit();
        }

        private void StopServer()
        {
            if (_server.IsRunning)
            {
                _server.Stop();
            }

            trayIcon.Icon = Resources.NotListeningIcon;
            listenForConnectionsToolStripMenuItem.Checked = false;
            statusLabel.Text = "Not listening";
            runningPicture.Visible = stopListeningButton.Visible = false;
            notRunningPicture.Visible = startListeningButton.Visible = true;
            trayIcon.Text = string.Format("smtp4dev (not listening)\n{1} messages", Settings.Default.PortNumber, _messages.Count);
        }

        private void viewLastMessageMenuItem_Click(object sender, EventArgs e)
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
                if (Settings.Default.InspectOnBalloonClick)
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

            foreach (SessionViewModel session in _sessions.Where(s => !_messages.Any(mvm => s.Session.Messages.Contains(mvm.Message))).ToArray())
            {
                _sessions.Remove(session);
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
            inspectMessageButton.Enabled =
                deleteButton.Enabled = viewButton.Enabled = saveButton.Enabled = SelectedMessages.Length > 0;
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
            if (WindowState == FormWindowState.Minimized && Properties.Settings.Default.MinimizeToSysTray)
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

            InspectorWindow form = new InspectorWindow(message.Parts);
            form.Show();

            messageGrid.Refresh();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            foreach (SessionViewModel session in SelectedSessions)
            {
                session.ViewLog();
            }
        }

        private void sessionsGrid_SelectionChanged(object sender, EventArgs e)
        {
            viewSessionButton.Enabled = deleteSessionButton.Enabled = SelectedSessions.Length > 0;
        }

        private void deleteSessionButton_Click(object sender, EventArgs e)
        {
            foreach (SessionViewModel session in SelectedSessions)
            {
                _sessions.Remove(session);

                foreach (MessageViewModel message in _messages.Where(mvm => session.Session.Messages.Any(m => mvm.Message == m)).ToArray())
                {
                    _messages.Remove(message);
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_quitting)
            {
                if (Settings.Default.MinimizeToSysTray)
                {
                    WindowState = FormWindowState.Minimized;
                    e.Cancel = true;
                }
            }
        }
    }
}