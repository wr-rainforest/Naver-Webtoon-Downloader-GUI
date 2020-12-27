using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using NaverWebtoonDownloader.CoreLib;
using NaverWebtoonDownloader.CoreLib.Database;

namespace NaverWebtoonDownloader.GUI
{
    class DownloadStatusViewModel : INotifyPropertyChanged
    {
        #region Constants
        private int GigaBytes => 1024 * 1024 * 1024;
        private int MegaBytes => 1024 * 1024;
        #endregion Constants

        #region Fields
        private string _latestEpisodeInfo;
        private string _statusMessage;
        private int _downloadedImageCount;
        private int _imageCount;
        private long _size;
        private Brush _background;
        private bool _isStartButtonEnabled;
        private bool _isStopButtonEnabled;

        private Config _config;
        private TaskQueue _taskQueue;
        private CancellationTokenSource _cts;
        #endregion

        #region Properties
        public Webtoon Webtoon { get; set; }
        public string Title => Webtoon.Title;
        public string Writer => Webtoon.Writer;
        public string LatestEpisodeInfo
        {
            get
            {
                return _latestEpisodeInfo;
            }
            set
            {
                _latestEpisodeInfo = value;
                OnPropertyChanged();
            }
        }
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
        public int DownloadedImageCount
        {
            get => _downloadedImageCount;
            set
            {
                _downloadedImageCount = value;
                OnPropertyChanged();
                OnPropertyChanged("Progress");
                OnPropertyChanged("ProgressText");
            }
        }
        public int ImageCount
        {
            get => _imageCount;
            set
            {
                _imageCount = value;
                OnPropertyChanged();
                OnPropertyChanged("Progress");
                OnPropertyChanged("ProgressText");
            }
        }
        public double Progress
        {
            get => DownloadedImageCount > 0 && ImageCount > 0
                ? (double)DownloadedImageCount / ImageCount
                : 0;
        }
        public string ProgressText
        {
            get => $"{Progress:P} [{DownloadedImageCount}/{ImageCount}]";
        }
        public long Size
        {
            get => _size;
            set
            {
                _size = value;
                OnPropertyChanged();
                OnPropertyChanged("SizeText");
            }
        }
        public string SizeText
        {
            get => _size > GigaBytes
                ? $"{(double)_size / GigaBytes:0.00} GB" 
                : $"{(double)_size / MegaBytes:0.00} MB";
        }
        public Brush Background
        {
            get => _background;
            set
            {
                _background = value;
                _background.Freeze();
                OnPropertyChanged();
            }
        }
        public bool IsStartButtonEnabled
        {
            get => _isStartButtonEnabled;
            set
            {
                _isStartButtonEnabled = value;
                OnPropertyChanged();
            }
        }
        public bool IsStopButtonEnabled
        {
            get => _isStopButtonEnabled;
            set
            {
                _isStopButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        public CancellationTokenSource Cts => _cts;
        #endregion Properties

        #region Commands
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand DeleteCommand { get; set; }
        #endregion Commands

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion INotifyPropertyChanged

        #region Func
        public DownloadStatusViewModel(Config config, TaskQueue taskQueue)
        {
            _config = config;
            _taskQueue = taskQueue;
            StartCommand = new AsyncCommand(x => StartAsync());
            StopCommand = new Command(x =>
            {
                _cts?.Cancel();
                StatusMessage = "취소중..";
            });
        }

        public async Task LoadAsync(Webtoon webtoon)
        {
            Webtoon = webtoon;
            IsStartButtonEnabled = false;
            IsStopButtonEnabled = false;
            LatestEpisodeInfo = await Task.Run(() =>
            {
                var context = new WebtoonDbContext();
                var query = from e in context.Episodes
                            where e.WebtoonID == webtoon.ID
                            orderby e.No
                            select e;
                if (!query.Any())
                    return "";
                var lastEpisode = query.Last();
                context.Dispose();
                return $"[{lastEpisode.Date:yyyy.MM.dd}] {lastEpisode.Title}";
            });
            await Task.Run(() =>
            {
                var context = new WebtoonDbContext();
                var query = from i in context.Images
                            where i.WebtoonID == webtoon.ID
                            select i.IsDownloaded;
                ImageCount = query.Count();
                DownloadedImageCount = query.Where(x => x).Count();
                context.Dispose();
            });
            Size = await Task.Run(() =>
            {
                var context = new WebtoonDbContext();
                var query = from i in context.Images
                            where i.WebtoonID == webtoon.ID && i.IsDownloaded
                            select i.Size;
                long sum = query.Sum();
                context.Dispose();
                return sum;
            });
            StatusMessage = "로딩 완료";
            IsStartButtonEnabled = true;
            IsStopButtonEnabled = false;
        }

        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            IsStartButtonEnabled = false;
            IsStopButtonEnabled = true;
            var updateProgress = new UpdateProgress(this);
            var downloadProgress = new DownloadProgress(this);
            var task = _taskQueue.Enqueue(async () =>
            {
                Downloader downloader = new Downloader(_config);
                StatusMessage = "회차 정보 동기화 진행중..";
                var updateTask = downloader.UpdateWebtoonDbAsync(Webtoon, updateProgress, _cts.Token);
                await updateTask;
                StatusMessage = "다운로드 진행중..";
                var downloadTask = downloader.DownloadAsync(Webtoon, downloadProgress, _cts.Token);
                await downloadTask;
            }, _cts.Token);

            Action handler = null;
            handler = () =>
            {
                int index = _taskQueue.IndexOf(task);
                if (index <= 0)
                {
                    _taskQueue.CollectionChanged -= handler;
                    return;
                }
                StatusMessage = $"작업 대기중..({index})";
            };
            _taskQueue.CollectionChanged += handler;
            handler();

            try
            {
                await task;
                StatusMessage = "다운로드 완료";
                downloadProgress.FinalizeStatus();
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "작업이 취소되었습니다.";
            }
            catch
            {
                StatusMessage = "작업 도중 오류가 발생했습니다";
                downloadProgress.Rollback();
            }
            finally
            {
                IsStartButtonEnabled = true;
                IsStopButtonEnabled = false;
                _cts = null;
            }
        }

        public async Task UpdateWebtoonDbAsync()
        {
            _cts = new CancellationTokenSource();
            IsStartButtonEnabled = false;
            IsStopButtonEnabled = true;

            Downloader downloader = new Downloader(_config);
            var task = _taskQueue.Enqueue(async () =>
            {
                StatusMessage = $"회차 정보 동기화 진행중..";
                await downloader.UpdateWebtoonDbAsync(Webtoon, new UpdateProgress(this), _cts.Token);
            }, _cts.Token);

            Action handler = null;
            handler = () =>
            {
                int index = _taskQueue.IndexOf(task);
                if (index <= 0)
                {
                    _taskQueue.CollectionChanged -= handler;
                    return;
                }
                StatusMessage = $"작업 대기중..({index})";
            };
            _taskQueue.CollectionChanged += handler;
            handler();

            try
            {
                await task;
                StatusMessage = $"회차 정보 동기화 완료";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "작업이 취소되었습니다.";
            }
            catch
            {
                StatusMessage = "작업 도중 오류가 발생했습니다";
            }
            finally
            {
                IsStartButtonEnabled = true;
                IsStopButtonEnabled = false;
                _cts = null;
            }
        }
        #endregion Func
    }
}
