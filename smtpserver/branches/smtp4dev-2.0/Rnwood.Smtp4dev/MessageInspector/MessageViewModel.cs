#region

using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using anmar.SharpMimeTools;
using Microsoft.Win32;
using System.Collections.Specialized;
using System.Collections;

#endregion

namespace Rnwood.Smtp4dev.MessageInspector
{
    public class MessageViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public MessageViewModel(SharpMimeMessage message)
        {
            Message = message;
        }

        public bool IsSelected
        {
            get { return _isSelected; }

            set
            {
                _isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }


        public SharpMimeMessage Message { get; private set; }

        public MessageViewModel[] Children
        {
            get { return Message.Cast<SharpMimeMessage>().Select(part => new MessageViewModel(part)).ToArray(); }
        }

        public HeaderViewModel[] Headers
        {
            get {
                return Message.Header.Cast<DictionaryEntry>().Select(de => new HeaderViewModel((string) de.Key, (string)de.Value)).ToArray(); }
        }

        public string Data
        {
            get { return Message.Header.RawHeaders + "\r\n\r\n" + Message.Body; }
        }

        public string Body
        {
            get { return Message.BodyDecoded; }
        }

        public string Type
        {
            get { return Message.Header.TopLevelMediaType + "/" + Message.Header.SubType; }
        }

        public long Size
        {
            get { return Message.Size; }
        }

        public string Disposition
        {
            get { return Message.Header.ContentDisposition; }
        }

        public string Encoding
        {
            get { return Message.Header.ContentTransferEncoding; }
        }

        public string Name
        {
            get { return Message.Name ?? "Unnamed" + ": " + MimeType + " (" + Message.Size + " bytes)"; }
        }

        protected string MimeType
        {
            get { return Message.Header.TopLevelMediaType + "/" + Message.Header.SubType; }
        }

        public string Subject
        {
            get { return Message.Header.Subject; }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public void Save()
        {
            SaveFileDialog dialog = new SaveFileDialog();

            string filename = (Message.Name ?? "Unnamed");

            if (string.IsNullOrEmpty(Path.GetExtension(Message.Name)))
            {
                filename = filename + (MIMEDatabase.GetExtension(MimeType));
            }

            dialog.FileName = filename;
            dialog.Filter = "File (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                using (FileStream stream = File.OpenWrite(dialog.FileName))
                {
                    Message.DumpBody(stream);
                }
            }
        }

        public void View()
        {
            string extn = Path.GetExtension(Message.Name ?? "Unnamed");

            if (string.IsNullOrEmpty(extn))
            {
                extn = MIMEDatabase.GetExtension(MimeType) ?? ".part";
            }

            TempFileCollection tempFiles = new TempFileCollection();
            FileInfo msgFile = new FileInfo(tempFiles.AddExtension(extn.TrimStart('.')));

            using (FileStream stream = msgFile.OpenWrite())
            {
                Message.DumpBody(stream);
            }

            Process.Start(msgFile.FullName);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }


    public class HeaderViewModel
    {
        public HeaderViewModel(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; private set; }
        public string Value { get; private set; }
    }
}