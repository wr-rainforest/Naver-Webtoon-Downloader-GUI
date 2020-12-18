using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NaverWebtoonDownloader;
using NaverWebtoonDownloader.CoreLib.Database;

namespace NaverWebtoonDownloader.GUI
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Menu
        public ICommand OpenDownloadFolderCommand { get; }

        public ICommand OpenSettingWindowCommand { get; set; }

        public ICommand OpenGithubCommand { get; set; }
        #endregion Menu

        #region Header
        public string UriTextBox { get; set; }

        public ICommand AddWebtoonCommand { get; set; }

        public async void AddWebtoonAsync()
        {
            if (string.IsNullOrWhiteSpace(UriTextBox))
            {
                MessageBox_Show("URI를 입력해 주세요", "웹툰 정보 확인 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Uri uri;
            if (!Uri.TryCreate(UriTextBox, UriKind.Absolute, out uri))
            {
                MessageBox_Show("URI 분석에 실패하였습니다.", "웹툰 정보 확인 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string titleId = HttpUtility.ParseQueryString(uri.Query).Get("titleId");
            if (string.IsNullOrEmpty(titleId) || int.TryParse(titleId, out int id))
            {
                MessageBox_Show("URI에서 웹툰 정보를 확인할 수 없습니다.", "웹툰 정보 확인 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var linq = from vm in DownloadStatusViewModels
                       where vm.Webtoon.ID == id
                       select vm;
            if (linq.Any())
            {
                MessageBox_Show("이미 추가된 웹툰입니다", "웹툰 추가 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Webtoon webtoon = await Model.Client.GetWebtoonAsync(id);
            using (var context = new WebtoonDbContext())
            {
                await context.Webtoons.AddAsync(webtoon);
                await context.SaveChangesAsync();
            }
            var downloadStatusViewModel = new DownloadStatusViewModel(webtoon);
            downloadStatusViewModel.Downloader = Model.Downloader;
            downloadStatusViewModel.MainWindowViewModel = this;
            downloadStatusViewModel.RegisterUpdateTask(Tasks);
            DownloadStatusViewModels.Add(downloadStatusViewModel);
        }
        #endregion Header

        #region Body
        public ObservableCollection<DownloadStatusViewModel> DownloadStatusViewModels { get; set; }
        #endregion Body

        #region Footer
        private string _footer1;
        public string Footer1
        { 
            get => _footer1;
            set
            {
                _footer1 = value;
                OnPropertyChanged();
            }
        }

        private string _footer2;
        public string Footer2
        {
            get => _footer2;
            set
            {
                _footer2 = value;
                OnPropertyChanged();
            }
        }
        #endregion Footer

        #region Func
        public Func<string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult> MessageBox_Show { get; set; }
        #endregion Func

        public List<Task> Tasks { get; set; } = new List<Task>();

        public MainWindowModel Model { get; set; }

        public MainWindowViewModel(MainWindowModel mainWindowModel)
        {
            OpenDownloadFolderCommand = new Command(() => Process.Start("explorer.exe", Model.Config.DownloadFolder));
            OpenSettingWindowCommand = new OpenSettingWindowCommand(Model.Config);
            OpenGithubCommand = new Command(() => Process.Start("cmd", "/c https://github.com/wr-rainforest/Naver-Webtoon-Downloader-GUI"));

            AddWebtoonCommand = new Command(AddWebtoonAsync);

            DownloadStatusViewModels = new ObservableCollection<DownloadStatusViewModel>();

            Model = mainWindowModel;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string info = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
        #endregion

    }
}
