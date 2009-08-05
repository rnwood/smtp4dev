using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rnwood.SmtpServer;
using System.CodeDom.Compiler;
using System.IO;
using System.Diagnostics;

namespace Rnwood.Smtp4dev
{
    public class SessionViewModel
    {
        public SessionViewModel(Session session)
        {
            Session = session;
        }

        public Session Session { get; private set; }

        public string Client
        {
            get
            {
                return Session.ClientAddress.ToString();
            }
        }

        public int NumberOfMessages
        {
            get
            {
                return Session.Messages.Count;
            }
        }

        public DateTime StartDate
        {
            get
            {
                return Session.StartDate;
            }
        }

        public void ViewLog()
        {
            TempFileCollection tempFiles = new TempFileCollection();
            FileInfo msgFile = new FileInfo(tempFiles.AddExtension("txt"));
            File.WriteAllText(msgFile.FullName, Session.Log ,Encoding.ASCII);
            Process.Start(msgFile.FullName); 
        }
    }
}
