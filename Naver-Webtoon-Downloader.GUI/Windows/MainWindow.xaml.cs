using NaverWebtoonDownloader.CoreLib;
using NaverWebtoonDownloader.CoreLib.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NaverWebtoonDownloader.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Dispatcher.UnhandledException += UnhandledException;
            Dispatcher.UnhandledExceptionFilter += UnhandledExceptionFilter;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var loadConfigTask = LoadConfig();
                var updateCheckTask = CheckUpdate();
                var viewModel = new MainWindowViewModel(new MainWindowModel(await loadConfigTask))
                {
                    MessageBox_Show = MessageBox.Show,
                    Footer1 = await updateCheckTask,
                };
                DataContext = viewModel;

                if (File.Exists(GlobalStatic.WebtoonDatabaseFilePath))
                {
                    var webtoons = await Task.Run(() =>
                    {
                        var context = new WebtoonDbContext();
                        var linq = from w in context.Webtoons
                                   select w;
                        return linq.ToList();
                    });

                    foreach (var webtoon in webtoons)
                    {
                        var downloadStatusViewModel = new DownloadStatusViewModel(webtoon);
                        downloadStatusViewModel.Downloader = viewModel.Model.Downloader;
                        downloadStatusViewModel.MainWindowViewModel = viewModel;
                        viewModel.DownloadStatusViewModels.Add(downloadStatusViewModel);
                        downloadStatusViewModel.RegisterUpdateTask(viewModel.Tasks);
                    }
                }
                else
                {
                    await Task.Run(() =>
                    {
                        using (var context = new WebtoonDbContext()) { }
                    });
                }
            }
            catch(Exception ex)
            {
                File.WriteAllText("error.log", ex.Message + "\r\n" + ex.StackTrace);
            }
        }

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
                catch
                {
                    MessageBox.Show("설정 파일 로딩에 실패하였습니다.");
                    throw;
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

        #region UnhandledException
        private void UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            File.WriteAllText("error.log", e.Exception.Message + "\r\n" + e.Exception.StackTrace);
        }

        private void UnhandledExceptionFilter(object sender, DispatcherUnhandledExceptionFilterEventArgs e)
        {
            MessageBox.Show(e.Exception.Message);
        }
        #endregion UnhandledException

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
    }
}
