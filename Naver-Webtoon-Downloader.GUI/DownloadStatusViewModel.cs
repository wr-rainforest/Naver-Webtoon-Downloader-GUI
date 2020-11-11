using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NaverWebtoonDownloader.CoreLib;

namespace NaverWebtoonDownloader.GUI
{
    class DownloadStatusViewModel : INotifyPropertyChanged
    {
        public static TaskManager taskManager;
        public static Downloader downloader;
        public static Dictionary<string, string> configs;
        public static Action<DownloadStatusViewModel> Remove { get; set; }
        public static Func<string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult> MessageBox_Show { get; set; }
        public DownloadStatusViewModel(WebtoonInfo webtoonInfo)
        {
            this.webtoonInfo = webtoonInfo;
            StartCommand = new Command(Start);
            deleteCommand = new Command(Delete);
            stopCommand = new Command(Stop);
            startButtonEnabledImage = new BitmapImage(new Uri("Resources/Run_16x.png", UriKind.Relative));
            startButtonDisabledImage = new BitmapImage(new Uri("Resources/Run_grey_16x.png", UriKind.Relative));
            stopButtonImage = new BitmapImage(new Uri("Resources/Stop_16x.png", UriKind.Relative));
            deleteButtonImage = new BitmapImage(new Uri("Resources/Cancel_16x.png", UriKind.Relative));
            startButtonDisabledImage.Freeze();
            stopButtonImage.Freeze();
            deleteButtonImage.Freeze();
            startButtonEnabledImage.Freeze();
        }

        private WebtoonInfo webtoonInfo;
        public WebtoonInfo WebtoonInfo { get => webtoonInfo; set => webtoonInfo = value; }
        public string Title => webtoonInfo.Title;
        public string TitleId => webtoonInfo.TitleId;
        public string Writer => webtoonInfo.Writer;
        private string latestEpisodeTitle;
        public string LatestEpisodeTitle
        {
            get => latestEpisodeTitle;
            set
            {
                latestEpisodeTitle = value;
                OnPropertyChanged();
            }
        }

        private string statusMessage;
        public string StatusMessage
        {
            get => statusMessage;
            set
            {
                statusMessage = value;
                OnPropertyChanged();
            }
        }

        private double progress;
        public double Progress
        {
            get => progress;
            set
            {
                progress = value;
                OnPropertyChanged();
            }
        }

        private string progressText;
        public string ProgressText
        {
            get => progressText;
            set
            {
                progressText = value;
                OnPropertyChanged();
            }
        }

        private long size;
        public long Size
        {
            get => size;
            set
            {
                size = value;
                OnPropertyChanged();
                OnPropertyChanged("SizeText");
            }
        }
        public string SizeText
        {
            get
            {
                if (size > 1024 * 1048576)
                    return $"{(double)size / (1024 * 1048576):0.00} GB";
                else
                    return $"{(double)size / 1048576:0.00} MB";
            }
        }

        private bool isChecked=false;
        public bool IsChecked
        {
            get => isChecked;
            set
            {
                isChecked = IsChecked;
                OnPropertyChanged();
            }
        }

        private Brush background;
        public Brush Background
        {
            get => background;
            set
            {
                background = value;
                background.Freeze();
                OnPropertyChanged();
            }
        }

        private bool isStartButtonEnabled = false;
        public bool IsStartButtonEnabled 
        {
            get=>isStartButtonEnabled;
            set
            {
                isStartButtonEnabled = value;
                OnPropertyChanged();
            }
        
        }
        private BitmapImage startButtonEnabledImage;
        private BitmapImage startButtonDisabledImage;
        public BitmapImage StartButtonImage 
        {
            get
            {
                if (isStartButtonEnabled)
                {
                    return startButtonEnabledImage;
                }
                else
                {
                    return startButtonDisabledImage;
                }
            }
        }

        private bool isStopDeleteButtonEnabled = true;
        public bool IsStopDeleteButtonEnabled
        {
            get => isStopDeleteButtonEnabled;
            set
            {
                isStopDeleteButtonEnabled = value;
                OnPropertyChanged();
            }

        }
        private StopDeleteButtonMode stopDeleteButtonMode =StopDeleteButtonMode.Delete;
        public StopDeleteButtonMode StopDeleteButtonMode 
        { 
            get=>stopDeleteButtonMode;
            set
            {
                stopDeleteButtonMode = value;
                OnPropertyChanged("StopDeleteButtonImage");
                OnPropertyChanged("StopDeleteCommand");
            }
        }
        private BitmapImage stopButtonImage;
        private BitmapImage deleteButtonImage;
        public BitmapImage StopDeleteButtonImage
        {
            get
            {
                if (stopDeleteButtonMode == StopDeleteButtonMode.Stop)
                {
                    return stopButtonImage;
                }
                else if (stopDeleteButtonMode == StopDeleteButtonMode.Delete)
                {
                    return deleteButtonImage;
                }
                else
                    throw new Exception();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string info = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        public ICommand StartCommand { get; set; }

        private Command stopCommand;
        private Command deleteCommand;
        public ICommand StopDeleteCommand 
        {
            get
            {
                if (stopDeleteButtonMode == StopDeleteButtonMode.Stop)
                    return stopCommand;
                else
                    return deleteCommand;
            } 
        }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        private void Start(object obj)
        {
            CancellationTokenSource = new CancellationTokenSource();
            IsStartButtonEnabled = false;
            StopDeleteButtonMode = StopDeleteButtonMode.Stop;
            int imageCount = downloader.Database.GetImageCount(webtoonInfo.TitleId);
            int size = downloader.Database.GetDownloadedImageSize(webtoonInfo.TitleId);
            int downloaded = downloader.Database.GetDownloadedImageCount(TitleId);
            Progress = (double)downloaded / imageCount;
            DownloadProgress progress = new DownloadProgress((args) =>
            {
                int position = (int)args[0];
                int size = (int)args[2];
                Progress = (double)(position + downloaded) / imageCount;
                ProgressText =
                $"{Progress:p} ( {position + downloaded} / {imageCount} )";
                Size += size;
            });
            CancellationToken ct = CancellationTokenSource.Token;
            Action download = new Action(() =>
            {
                StatusMessage = "다운로드 진행중..";
                if(!ct.IsCancellationRequested)
                    downloader.DownloadWebtoonAsync(TitleId, configs["DefaultDownloadFolder"], progress, ct).Wait();
                IsStartButtonEnabled = true;
                StopDeleteButtonMode = StopDeleteButtonMode.Delete;
                if (!ct.IsCancellationRequested)
                {
                    Progress = 1d;
                    ProgressText =
                    $"{Progress:p} ( {imageCount} / {imageCount} )";
                    StatusMessage = "다운로드 완료";
                }
                else
                {
                    StatusMessage = "다운로드가 취소되었습니다.";
                }
            });
            var guid = taskManager.Register(download, CancellationTokenSource.Token);
            Action indexChanged = null;
            indexChanged = new Action(() =>
            {
                int index = taskManager.IndexOf(guid);
                if (index < 0)
                {
                    taskManager.IndexChangedEvent -= indexChanged;
                }
                StatusMessage = $"작업 대기중...({index + 1})";
            });
            taskManager.IndexChangedEvent += indexChanged;
        }

        private void Stop(object obj)
        {
            CancellationTokenSource?.Cancel();
        }

        private void Delete(object obj)
        {
            var result = MessageBox_Show($"'{Title}'을/를 목록에서 삭제할까요?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Remove(this);
            }
        }

        public void Update()
        {
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken ct = CancellationTokenSource.Token;
            IsStartButtonEnabled = false;
            IsStopDeleteButtonEnabled = false;
            StopDeleteButtonMode = StopDeleteButtonMode.Stop;
            var progress = new UpdateProgress((tuple) =>
            {
                Progress = (double)tuple.position / tuple.count;
                ProgressText =
                $"{Progress:p} ( {tuple.position} / {tuple.count} )";
            });
            
            Action update = new Action(() =>
            {
                StatusMessage = "데이터베이스 업데이트중..";
                if (!ct.IsCancellationRequested)
                    downloader.UpdateOrInsertWebtoonDatabase(webtoonInfo.TitleId, progress, ct).Wait();
                IsStartButtonEnabled = true;
                IsStopDeleteButtonEnabled = true;
                StopDeleteButtonMode = StopDeleteButtonMode.Delete;
                if (!CancellationTokenSource.IsCancellationRequested)
                {
                    StatusMessage = "데이터베이스 업데이트 완료";
                    int imageCount = downloader.Database.GetImageCount(webtoonInfo.TitleId);
                    int downloaded = downloader.Database.GetDownloadedImageCount(webtoonInfo.TitleId);
                    int size = downloader.Database.GetDownloadedImageSize(webtoonInfo.TitleId);
                    Progress = (double)downloaded / imageCount;
                    ProgressText =
                    $"{Progress:P} ( {downloaded} / {imageCount} )";
                    Size = size;
                }
                else
                {
                    StatusMessage = "작업이 취소되었습니다.";
                }
            });
            var guid = taskManager.Register(update, CancellationTokenSource.Token);
            Action indexChanged = null;
            indexChanged = new Action(() =>
            {
                int index = taskManager.IndexOf(guid);
                if (index < 0)
                {
                    taskManager.IndexChangedEvent -= indexChanged;
                }
                StatusMessage = $"작업 대기중...({index+1})";
            });
            taskManager.IndexChangedEvent += indexChanged;
        }
    }

    enum StopDeleteButtonMode
    {
        Stop = 0,
        Delete = 1
    }
}
