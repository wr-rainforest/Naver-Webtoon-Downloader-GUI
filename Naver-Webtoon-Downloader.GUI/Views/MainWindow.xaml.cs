using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NaverWebtoonDownloader.CoreLib;

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
            Dispatcher.UnhandledExceptionFilter += UnhandledExceptionFilter;
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"Naver-Webtoon-Downloader-GUI v{version.Major}.{version.Minor}";
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var loadConfigTask = LoadConfig();
            var updateCheckTask = CheckUpdate();
            var viewModel = new MainWindowViewModel(await loadConfigTask)
            {
                MessageBox_Show = new Func<string, MessageBoxButton, MessageBoxImage, MessageBoxResult>((s, b, i) => MessageBox.Show(s, "Naver-Webtoon-Downloader-GUI", b, i)),
                Footer1 = await updateCheckTask,
                OpenSettingWindowCommand = new Command(x =>
                {
                    SettingWindow settingWindow = new SettingWindow()
                    {
                        Owner = this,
                    };
                    settingWindow.DataContext = new SettingWindowViewModel()
                    {
                        MessageBox_Show = new Func<string, MessageBoxButton, MessageBoxImage, MessageBoxResult>((s, b, i) => MessageBox.Show(s, "Naver-Webtoon-Downloader-GUI", b, i)),
                        MessageBox_Show_ErrorDialog = new Action<string>(s => MessageBox.Show(s, "Naver-Webtoon-Downloader-GUI", MessageBoxButton.OK, MessageBoxImage.Warning)),
                    };
                    settingWindow.ShowDialog();
                }),
                OpenInformationWindowCommand = new Command(x =>
                {
                    InformationWindow informationWindow = new InformationWindow()
                    {
                        Owner = this,
                        DataContext = new InformationWindowViewModel()
                    };
                    informationWindow.Show();
                }),
                OpenCookieSettingWindowCommand = new Command(x =>
                {
                    CookieSettingWindow cookieSettingWindow = new CookieSettingWindow()
                    {
                        Owner = this,
                    };
                    cookieSettingWindow.Show();
                }),
            };
            DataContext = viewModel;
            await viewModel.LoadAsync();
        }

        #region Loading
        private async Task<string> CheckUpdate()
        {
            Version latestVersion;
            var client = new HttpClient();
            try
            {
                var versionString = await client.GetStringAsync("https://raw.githubusercontent.com/wr-rainforest/Naver-Webtoon-Downloader-GUI/master/Pages/version.info.0.2.txt");
                latestVersion = new Version(versionString);
            }
            catch (Exception e)
            {
                MessageBox.Show($"버전 정보를 불러오는데 실패하였습니다.\r\n({e.Message})", "버전 체크 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                return "버전 체크 실패";
            }

            switch (Assembly.GetExecutingAssembly().GetName().Version.CompareTo(latestVersion))
            {
                case -1:
                    MessageBox.Show($"새로운 버전이 출시되었습니다.({latestVersion.Major}.{latestVersion.Minor})", "업데이트 안내", MessageBoxButton.OK, MessageBoxImage.Information);
                    return "새로운 버전이 출시되었습니다";
                case 0:
                    return "최신 버전입니다.";
                case 1:
                    return "개발 버전입니다.";
                default:
                    MessageBox.Show("unk");
                    throw new Exception();
            }
        }

        private async Task<Config> LoadConfig()
        {
            Config config;
            if (File.Exists(GlobalStatic.ConfigFilePath))
            {
                try
                {
                    config = JsonSerializer.Deserialize<Config>(
                        await File.ReadAllTextAsync(GlobalStatic.ConfigFilePath));
                }
                catch(Exception e)
                {
                    throw new FileLoadException("설정 파일 로딩에 실패하였습니다.", e);
                }
            }
            else
            {
                config = new Config();
                await File.WriteAllTextAsync(
                    GlobalStatic.ConfigFilePath,
                    JsonSerializer.Serialize(config, new JsonSerializerOptions() { WriteIndented = true }));
            }
            return config;
        }
        #endregion Loading

        #region UnhandledException
        private void UnhandledExceptionFilter(object sender, DispatcherUnhandledExceptionFilterEventArgs e)
        {
            e.RequestCatch = false;
            MessageBox.Show(
                $"{e.Exception.GetType()}: {e.Exception.Message}",
                "Naver-Webtoon-Downloader-GUI",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            File.WriteAllText("error.log", e.Exception.ToString());
        }
        #endregion UnhandledException

        #region UI Event
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
        #endregion UI Event
    }
}
