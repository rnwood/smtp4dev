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

namespace Rnwood.Smtp4dev.MessageInspector
{
    /// <summary>
    /// Interaction logic for EmailInspectorWindow.xaml
    /// </summary>
    public partial class InspectorWindow : Window
    {
        public InspectorWindow(SharpMimeMessage message)
        {
            InitializeComponent();
            Message = new MessageViewModel(message);
        }

        public MessageViewModel Message
        {
            get
            {
                return DataContext as MessageViewModel;
            }

            private set
            {
                DataContext = value;

                treeView.DataContext = new MessageViewModel[] { Message };

                string tempFile = Path.GetTempFileName()+".mhtml";
                File.WriteAllText(tempFile, Message.ToString(), Encoding.Default);
                messageWebBrowser.Navigate(new Uri("file:///" + tempFile));
            }
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (treeView.SelectedItem != null)
            {
                partDetailsGrid.IsEnabled = true;
                string tempFile = Path.GetTempFileName() + ".mhtml";
                File.WriteAllText(tempFile, ((MessageViewModel)treeView.SelectedItem).Data ?? "", Encoding.Default);
                partWebBrowser.Navigate(new Uri("file:///" + tempFile));
            } else
            {
                partDetailsGrid.IsEnabled = false;
                partWebBrowser.Navigate(new Uri("about:blank"));
            }
        }
    }
}
