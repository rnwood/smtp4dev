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
                SelectedPart = Message;
            }
        }

        public MessageViewModel SelectedPart
        {
            get
            {
                return treeView.SelectedItem as MessageViewModel;
            }

            set
            {
                value.IsSelected = true;
            }
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (treeView.SelectedItem != null)
            {
                partDetailsGrid.IsEnabled = true;
            } else
            {
                partDetailsGrid.IsEnabled = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedPart.View();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SelectedPart.Save();
        }
    }
}
