using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using anmar.SharpMimeTools;
using Path=System.IO.Path;

namespace smtp4dev
{
    /// <summary>
    /// Interaction logic for EmailInspectorWindow.xaml
    /// </summary>
    public partial class EmailInspectorWindow : Window
    {
        public EmailInspectorWindow(Email email)
        {
            InitializeComponent();
            Email = email;
        }

        public Email Email
        {
            get
            {
                return DataContext as Email;
            }

            private set
            {
                DataContext = value;
                
                MemoryStream stream = new MemoryStream(Encoding.Default.GetBytes(Email.Envelope.Data));
                stream.Position = 0;
                treeView.DataContext = new MessagePartNode[] {new MessagePartNode(new SharpMimeMessage(stream))};

                string tempFile = Path.GetTempFileName()+".mhtml";
                File.WriteAllText(tempFile, Email.Envelope.Data ?? "", Encoding.Default);
                messageWebBrowser.Navigate(new Uri("file:///" + tempFile));
            }
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (treeView.SelectedItem != null)
            {
                string tempFile = Path.GetTempFileName() + ".mhtml";
                File.WriteAllText(tempFile, ((MessagePartNode)treeView.SelectedItem).Data ?? "", Encoding.Default);
                partWebBrowser.Navigate(new Uri("file:///" + tempFile));
            } else
            {
                partWebBrowser.Navigate(new Uri("about:blank"));
            }
        }
    }

    class MessagePartNode
    {
        public MessagePartNode(SharpMimeMessage sharpMimeMessage)
        {
            SharpMimeMessage = sharpMimeMessage;
        }

        public string Data
        {
            get
            {
                return SharpMimeMessage.Header.RawHeaders + "\r\n\r\n" + SharpMimeMessage.Body;
            }
        }

        public string Body
        {
            get
            {
                return SharpMimeMessage.BodyDecoded;
            }
        }

        public string Type
        {
            get
            {
                return SharpMimeMessage.Header.TopLevelMediaType + "/" + SharpMimeMessage.Header.SubType;
            }
        }

        public long Size
        {
            get
            {
                return SharpMimeMessage.Size;
            }
        }

        public string Disposition
        {
            get
            {
                return SharpMimeMessage.Header.ContentDisposition;
            }
        }

        public string Encoding
        {
            get
            {
                return SharpMimeMessage.Header.ContentTransferEncoding;
            }
        }

        public string Name
        {
            get
            {
                return SharpMimeMessage.Name + " (" + SharpMimeMessage.Header.TopLevelMediaType + "/" + SharpMimeMessage.Header.SubType + ")";
            }
        }

        public SharpMimeMessage SharpMimeMessage { get; private set; }

        public MessagePartNode[] Children
        {
            get
            {
                return SharpMimeMessage.Cast<SharpMimeMessage>().Select(part => new MessagePartNode(part)).ToArray();
            }
        }
    }
}
