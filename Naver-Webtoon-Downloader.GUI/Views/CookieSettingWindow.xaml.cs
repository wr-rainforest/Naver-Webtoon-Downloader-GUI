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
using System.Windows.Shapes;

namespace NaverWebtoonDownloader.GUI
{
    /// <summary>
    /// CookieSettingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CookieSettingWindow : Window
    {
        public CookieSettingWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = new CookieSettingWindowViewModel()
            {
                MessageBox_Show_ErrorDialog = new Action<string>(s => MessageBox.Show(s, "Naver-Webtoon-Downloader-GUI", MessageBoxButton.OK, MessageBoxImage.Warning)),
                MessageBox_Show = new Func<string, MessageBoxButton, MessageBoxImage, MessageBoxResult>((s, b, i) => MessageBox.Show(s, "Naver-Webtoon-Downloader-GUI", b, i)),
                SaveCookieEnabled = true,
            };
            await viewModel.LoadAsync();
            DataContext = viewModel;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
