using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using NaverWebtoonDownloader.CoreLib;
using NaverWebtoonDownloader.CoreLib.Database;

namespace NaverWebtoonDownloader.GUI
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Fields
        private bool _isLoading;
        private string _uriTextBox;
        private bool _isAddWebtoonButtonEnabled;
        private string _footer1;
        private string _footer2;
        private Config _config;
        private TaskQueue _taskQueue;
        #endregion Fields

        #region Properties
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
        public string UriTextBox
        {
            get => _uriTextBox;
            set
            {
                _uriTextBox = value;
                OnPropertyChanged();
            }
        }
        public bool IsAddWebtoonButtonEnabled
        {
            get => _isAddWebtoonButtonEnabled;
            set
            {
                _isAddWebtoonButtonEnabled = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<DownloadStatusViewModel> DownloadStatusViewModels { get; set; }
        public string Footer1
        {
            get => _footer1;
            set
            {
                _footer1 = value;
                OnPropertyChanged();
            }
        }
        public string Footer2
        {
            get => _footer2;
            set
            {
                _footer2 = value;
                OnPropertyChanged();
            }
        }
        #endregion Properties

        #region Commands
        public ICommand OpenDownloadFolderCommand { get; }
        public ICommand OpenCookieSettingWindowCommand { get; set; }
        public ICommand OpenSettingWindowCommand { get; set; }
        public ICommand OpenGithubCommand { get; }
        public ICommand OpenInformationWindowCommand { get; set; }
        public ICommand AddWebtoonCommand { get; set; }
        #endregion Commands

        #region Event
        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshBackground();
        }

        private void RefreshBackground()
        {
            int i = 0;
            foreach (var item in DownloadStatusViewModels)
            {
                if (i % 2 == 0)
                    item.Background = Brushes.White;
                else
                    item.Background = new SolidColorBrush(Color.FromRgb(0xf0, 0xf0, 0xf0));
                i++;
            };
        }
        #endregion Event

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion INotifyPropertyChanged

        #region Func
        public MainWindowViewModel(Config config)
        {
            _config = config;
            _taskQueue = new TaskQueue();
            OpenDownloadFolderCommand = new Command(x => Process.Start("explorer.exe", Path.GetFullPath(_config.DownloadFolder)));
            OpenGithubCommand = new Command(x => Process.Start(new ProcessStartInfo("cmd", "/c start https://github.com/wr-rainforest/Naver-Webtoon-Downloader-GUI") { CreateNoWindow = true }));
            AddWebtoonCommand = new AsyncCommand(x => AddWebtoonAsync());
            IsAddWebtoonButtonEnabled = false;
            DownloadStatusViewModels = new ObservableCollection<DownloadStatusViewModel>();
            DownloadStatusViewModels.CollectionChanged += CollectionChanged;
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            if (File.Exists(GlobalStatic.WebtoonDatabaseFilePath))
            {
                IEnumerable<Webtoon> webtoons;
                using (var context = new WebtoonDbContext())
                {
                    var query = from w in context.Webtoons.AsNoTracking()
                                orderby w.Title
                                select w;
                    webtoons = await query.ToListAsync();
                }
                List<Task> updateTasks = new List<Task>();
                foreach (var webtoon in webtoons)
                {
                    var downloadStatusViewModel = new DownloadStatusViewModel(_config, _taskQueue)
                    {
                        DeleteCommand = new AsyncCommand(x => DeleteAsync((DownloadStatusViewModel)x)),
                        StatusMessage = "로딩중.."
                    }; 
                    await downloadStatusViewModel.LoadAsync(webtoon);
                    DownloadStatusViewModels.Add(downloadStatusViewModel);
                    var updateTask = downloadStatusViewModel.UpdateWebtoonDbAsync();
                    updateTasks.Add(updateTask);
                }
                IsLoading = false;
                await Task.WhenAll(updateTasks);
            }
            else
            {
                await Task.Run(() =>
                {
                    using (var context = new WebtoonDbContext()) { }
                });
                IsLoading = false;
            }
            IsAddWebtoonButtonEnabled = true;
        }

        public Func<string, MessageBoxButton, MessageBoxImage, MessageBoxResult> MessageBox_Show { get; set; }

        public async Task AddWebtoonAsync()
        {
            if (string.IsNullOrWhiteSpace(UriTextBox))
            {
                MessageBox_Show("URI를 입력해 주세요", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Uri uri;
            if (!Uri.TryCreate(UriTextBox, UriKind.Absolute, out uri))
            {
                MessageBox_Show("URI 분석에 실패하였습니다.",  MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string titleId = HttpUtility.ParseQueryString(uri.Query).Get("titleId");
            if (string.IsNullOrEmpty(titleId) || !int.TryParse(titleId, out int id))
            {
                MessageBox_Show("URI에서 웹툰 정보를 확인할 수 없습니다.", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var linq = from vm in DownloadStatusViewModels
                       where vm.Webtoon.ID == id
                       select vm;
            if (linq.Any())
            {
                MessageBox_Show("이미 추가된 웹툰입니다", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Webtoon webtoon = await new NaverWebtoonClient().GetWebtoonAsync(id);
            using (var context = new WebtoonDbContext())
            {
                await context.Webtoons.AddAsync(webtoon);
                await context.SaveChangesAsync();
            }
            var downloadStatusViewModel = new DownloadStatusViewModel(_config, _taskQueue)
            {
                DeleteCommand = new AsyncCommand(x => DeleteAsync((DownloadStatusViewModel)x)),
            };
            var loadingTask = downloadStatusViewModel.LoadAsync(webtoon);
            DownloadStatusViewModels.Add(downloadStatusViewModel);
            await loadingTask;
            await downloadStatusViewModel.UpdateWebtoonDbAsync();
        }

        private async Task DeleteAsync(DownloadStatusViewModel model)
        {
            model.Cts?.Cancel();
            MessageBoxResult result;
            result = MessageBox_Show($"'{model.Webtoon.Title}'을/를 목록에서 삭제할까요?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
                return;
            DownloadStatusViewModels.Remove(model);
            using (var context = new WebtoonDbContext())
            {
                context.Webtoons.Remove(model.Webtoon);
                await context.SaveChangesAsync();
            }
        }
        #endregion
    }
}
