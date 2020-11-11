using Microsoft.Data.Sqlite;
using NaverWebtoonDownloader.CoreLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NaverWebtoonDownloader.GUI
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        public Func<string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult> MessageBox_Show { get; set; }
        private Downloader downloader;
        public ObservableCollection<DownloadStatusViewModel> Downloads { get; set; }
        private string uri="";
        public string Uri { get=>uri; set { uri = value;OnPropertyChanged(); } }
        private string footer1;
        public string Footer1 { get=>footer1; set { footer1 = value; OnPropertyChanged(); } }
        public ICommand AddWebtoonCommand { get; set; }
        public ICommand OpenDefaultDownloadFolderCommand { get; set; }
        public ICommand OpenGithubCommand { get; set; }
        public ICommand SettingCommand { get; set; }
        NameFormat nameFormat = new NameFormat();
        Dictionary<string, string> configs = new Dictionary<string, string>();

        private void LoadAppData()
        {
            var appdataFolder = Directory.CreateDirectory("AppData");
            var connection = new SqliteConnection($"Data Source={Path.Combine(appdataFolder.FullName, "appdata.db")}");
            connection.Open();
            var selectMasterCommand = connection.CreateCommand();
            selectMasterCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
            var tableNameReader = selectMasterCommand.ExecuteReader();
            bool configTableExists = false;
            while (tableNameReader.Read())
            {
                if ("configs".Equals((string)tableNameReader["name"]))
                {
                    configTableExists = true;
                }
            }
            if (!configTableExists)
            {
                var createConfigsTable = connection.CreateCommand();
                createConfigsTable.CommandText =
                    "BEGIN;" +
                    "CREATE TABLE configs (key, value, PRIMARY KEY(key));" +
                    "INSERT into configs (key, value) VALUES ('DefaultDownloadFolder', @DefaultDownloadFolder);" +
                    "INSERT into configs (key, value) VALUES ('WebtoonDirectoryNameFormat', @WebtoonDirectoryNameFormat);" +
                    "INSERT into configs (key, value) VALUES ('EpisodeDirectoryNameFormat', @EpisodeDirectoryNameFormat);" +
                    "INSERT into configs (key, value) VALUES ('ImageFileNameFormat', @ImageFileNameFormat);" +
                    "COMMIT;";
                createConfigsTable.Parameters.AddWithValue("@DefaultDownloadFolder", "Downloads");
                createConfigsTable.Parameters.AddWithValue("@WebtoonDirectoryNameFormat", nameFormat.WebtoonDirectoryNameFormat);
                createConfigsTable.Parameters.AddWithValue("@EpisodeDirectoryNameFormat", nameFormat.EpisodeDirectoryNameFormat);
                createConfigsTable.Parameters.AddWithValue("@ImageFileNameFormat", nameFormat.ImageFileNameFormat);
                createConfigsTable.ExecuteNonQuery();
            }
            var selectConfigsCommand = connection.CreateCommand();
            selectConfigsCommand.CommandText = "SELECT * FROM configs";
            var configReader = selectConfigsCommand.ExecuteReader();
            while (configReader.Read())
            {
                configs.Add(configReader["key"] as string, configReader["value"] as string);
            }
        }

        private async Task CheckVersion()
        {
            Version latestVersion;
            try
            {
                var versionString = await Downloader.Client.GetStringAsync("https://raw.githubusercontent.com/wr-rainforest/Naver-Webtoon-Downloader-GUI/master/Pages/version.info.0.2.txt");
                latestVersion = new Version(versionString);
            }
            catch(Exception e)
            {
                Footer1 = "버전 체크 실패";
                MessageBox_Show($"버전 정보를 불러오는데 실패하였습니다.\r\n({e.Message})", "버전 체크 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Version currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            int compareResult = currentVersion.CompareTo(latestVersion);
            if (compareResult < 0)
            {
                MessageBox_Show($"새로운 버전이 출시되었습니다.({latestVersion.Major}.{latestVersion.Minor})","업데이트 안내", MessageBoxButton.OK, MessageBoxImage.Information);
                Footer1 = "새로운 버전이 출시되었습니다";
            }
            else if (compareResult == 0)
            {
                Footer1 = "최신 버전입니다.";
            }
            else
            {
                Footer1 = "개발 버전입니다.";
            }
        }

        public void LoadWebtoons()
        {
            var webtoons = downloader.Database.SelectWebtoons();
            if (webtoons.Length == 0)
            {
                return;
            }
            foreach(var webtoonInfo in webtoons)
            {
                var downloadStatusViewModel=new DownloadStatusViewModel(webtoonInfo);
                Application.Current.Dispatcher.Invoke(new Action(()=>Downloads.Add(downloadStatusViewModel)));
                RefreshBackground();
                downloadStatusViewModel.Update();
            }
        }

        Thread loadingThread;
        public MainWindowViewModel(Func<string, string, MessageBoxButton, MessageBoxImage,MessageBoxResult> messageBox_Show)
        {
            MessageBox_Show = messageBox_Show;
            Downloads = new ObservableCollection<DownloadStatusViewModel>();
            AddWebtoonCommand = new Command(AddWebtoon);
            OpenDefaultDownloadFolderCommand = new Command((obj) => Process.Start(Path.GetFullPath(configs["DefaultDownloadFolder"])));
            OpenGithubCommand = new Command((obj) => Process.Start("https://github.com/wr-rainforest/Naver-Webtoon-Downloader-GUI"));
            SettingCommand = new Command(Setting);
            DownloadStatusViewModel.taskManager = new TaskManager(Thread.CurrentThread);
            loadingThread = new Thread(async () =>
            {
                downloader = new Downloader("database.db", new NameFormat());
                LoadAppData();
                DownloadStatusViewModel.configs = configs;
                DownloadStatusViewModel.downloader = downloader;
                DownloadStatusViewModel.MessageBox_Show = messageBox_Show;
                DownloadStatusViewModel.Remove = Remove;
                    LoadWebtoons();
                await CheckVersion();
            });
            loadingThread.Start();
        }

        private void Remove(DownloadStatusViewModel item)
        {
            Downloads.Remove(item);
            downloader.Database.DeleteWebtoon(item.TitleId);
            RefreshBackground();
        }

        public void Setting(object obj)
        {

        }

        public async void AddWebtoon(object obj)
        {
            if (string.IsNullOrWhiteSpace(Uri))
            {
                MessageBox_Show("URI를 입력해 주세요", "웹툰 정보 확인 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Uri uri;
            if (!System.Uri.TryCreate(Uri, UriKind.Absolute, out uri))
            {
                MessageBox_Show("URI 분석에 실패하였습니다.", "웹툰 정보 확인 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string titleId = HttpUtility.ParseQueryString(uri.Query).Get("titleId");
            if(string.IsNullOrEmpty(titleId))
            {
                MessageBox_Show("URI에서 웹툰 정보를 확인할 수 없습니다.", "웹툰 정보 확인 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            foreach(var item in Downloads)
            {
                if (item.TitleId == titleId)
                {
                    MessageBox_Show("이미 추가된 웹툰입니다", "웹툰 추가 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            var msg = await downloader.CanDownload(titleId);
            if (msg != null)
            {
                MessageBox_Show(msg, "다운로드 불가능", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var webtoonInfo = await Downloader.Client.GetWebtoonInfoAsync(titleId);
            var downloadStatusViewModel = new DownloadStatusViewModel(webtoonInfo);
            Downloads.Add(downloadStatusViewModel);
            RefreshBackground();
            downloader.Database.InsertWebtoon(webtoonInfo);
            downloadStatusViewModel.Update();
        }

        private void RefreshBackground()
        {
            int i = 0;
            foreach (var item in Downloads)
            {
                if (i % 2 == 0)
                    item.Background = Brushes.White;
                else
                    item.Background= new SolidColorBrush(Color.FromRgb(0xf0, 0xf0, 0xf0));
                i++;
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string info = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
