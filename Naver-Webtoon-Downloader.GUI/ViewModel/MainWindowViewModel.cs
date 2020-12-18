using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
            string uri = UriTextBox;
            int id = 0;
            var linq = from vm in DownloadStatusViewModels
                       where vm.Webtoon.ID == id
                       select vm;
            if (linq.Any())
            {
                return;
            }

            Webtoon webtoon = await Model.Client.GetWebtoonAsync(id);
            using (var context = new WebtoonDbContext())
            {
                await context.Webtoons.AddAsync(webtoon);
                await context.SaveChangesAsync();
            }
            var downloadStatusViewModel = new DownloadStatusViewModel(webtoon);
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
            Model = mainWindowModel;
            DownloadStatusViewModels = new ObservableCollection<DownloadStatusViewModel>();
            OpenDownloadFolderCommand = new Command(() => Process.Start("explorer.exe", Model.Config.DownloadFolder));
            OpenSettingWindowCommand = new OpenSettingWindowCommand(Model.Config);
            OpenGithubCommand = new Command(() => Process.Start("cmd", "/c https://github.com/wr-rainforest/Naver-Webtoon-Downloader-GUI"));
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
