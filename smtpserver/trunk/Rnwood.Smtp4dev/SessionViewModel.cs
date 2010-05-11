#region

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Text;
using Rnwood.SmtpServer;

#endregion

namespace Rnwood.Smtp4dev
{
    public class SessionViewModel
    {
        public SessionViewModel(ISession session)
        {
            Session = session;
        }

        public ISession Session { get; private set; }

        public bool SecureConnection
        {
            get
            {
                return Session.SecureConnection;
            }
        }

        public string Client
        {
            get { return Session.ClientAddress.ToString(); }
        }

        public int NumberOfMessages
        {
            get { return Session.Messages.Count; }
        }

        public DateTime StartDate
        {
            get { return Session.StartDate; }
        }

        public void ViewLog()
        {
            TempFileCollection tempFiles = new TempFileCollection();
            FileInfo msgFile = new FileInfo(tempFiles.AddExtension("txt"));
            File.WriteAllText(msgFile.FullName, Session.Log, Encoding.ASCII);
            Process.Start(msgFile.FullName);
        }
    }
}