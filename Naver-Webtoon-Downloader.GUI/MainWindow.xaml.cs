using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Data;

namespace NaverWebtoonDownloader.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var mainWindowViewModel = new MainWindowViewModel(new Func<string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult>(
                (arg1, arg2, arg3, arg4) =>
                {
                    return Dispatcher.Invoke(new Func<MessageBoxResult>(() => MessageBox.Show(this, arg1, arg2, arg3, arg4)));
                }));
            //mainWindowViewModel.
            DataContext = mainWindowViewModel;
        }

        private void UriTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            UriTextBox.Focus();
            if (e.Key == Key.Return)
            {
                var mainWindowViewModel = (sender as TextBox).DataContext as MainWindowViewModel;
                if (!mainWindowViewModel.AddWebtoonCommand.CanExecute(null))
                    return;
                else
                    mainWindowViewModel.AddWebtoonCommand.Execute(null);
            }
        }

        private void SettingMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
