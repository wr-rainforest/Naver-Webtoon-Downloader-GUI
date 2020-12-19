using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
using NaverWebtoonDownloader.CoreLib;
using NaverWebtoonDownloader.CoreLib.Database;

namespace NaverWebtoonDownloader.GUI
{
    class DownloadStatusViewModel : INotifyPropertyChanged
    {
        public Webtoon Webtoon { get; }

        public MainWindowViewModel MainWindowViewModel { get; set; }

        #region Property
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        public string Title { get => Webtoon.Title; }

        public string Writer { get => Webtoon.Writer; }

        private string _latestEpisode;
        public string LatestEpisode
        {
            get => _latestEpisode;
            set 
            {
                _latestEpisode = value;
                OnPropertyChanged();
            }
        }

        private string _status;
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        private double _progress;
        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        private string _progressMessage;
        public string ProgressMessage
        {
            get => _progressMessage;
            set
            {
                _progressMessage = value;
                OnPropertyChanged();
            } 
        }

        private long _size;
        public long Size
        {
            get => _size;
            set 
            {
                _size = value;
                SizeText = _size > 1024 * 1024 * 1024
                           ? $"{(double)_size / 1024 * 1024 * 1024:0.00} GB"
                           : $"{(double)_size / 1024 * 1024:0.00} MB";
            } 
        }

        private string _sizeText;
        public string SizeText
        {
            get => _sizeText;
            private set
            {
                _sizeText = value;
                OnPropertyChanged();
            }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }

        private CancellationTokenSource _cts;
        private CancellationTokenSource Cts
        {
            get => _cts;
            set
            {
                _cts = value;
                if (value == null)
                    IsRunning = false;
                else
                    IsRunning = true;
                OnPropertyChanged("IsRunning");
            }
        }

        private Brush _background;
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
        #endregion Property

        #region Command
        public ICommand StartCommand { get; set; }

        public ICommand StopCommand { get; set; }

        public ICommand DeleteCommand { get; set; }
        #endregion

        #region Command Func
        public void Start()
        {
            RegisterDownloadTask(MainWindowViewModel.Tasks);
        }

        public void Stop()
        {
            Cts.Cancel();
            Cts = null;
        }

        public void Delete()
        {
            Cts?.Cancel();
            MessageBoxResult result;
            result = MainWindowViewModel.MessageBox_Show($"'{Title}'을/를 목록에서 삭제할까요?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
                return;
            MainWindowViewModel.DownloadStatusViewModels.Remove(this);
            using (var context = new WebtoonDbContext())
            {
                context.Webtoons.Remove(Webtoon);
            }
        }
        #endregion

        public DownloadStatusViewModel(Webtoon webtoon)
        {
            Webtoon = webtoon;
            OnPropertyChanged("Title");
            OnPropertyChanged("Writer");

            StartCommand = new Command(Start);
            StopCommand = new Command(Stop);
            DeleteCommand = new Command(Delete);
        }

        public void RegisterDownloadTask(List<Task> taskList)
        {
            Cts = new CancellationTokenSource();
            CancellationToken ct = Cts.Token;

            Task task = null;
            task = Task.Run(async () =>
            {
                var client = new NaverWebtoonClient();
                int latestNo = await client.GetLatestEpisodeNoAsync((int)Webtoon.ID);
                int lastNo;
                using (var context = new WebtoonDbContext())
                {
                    var linq = from e in context.Episodes
                               where e.WebtoonID == Webtoon.ID
                               select e.No;
                    if (!linq.Any())
                        lastNo = 0;
                    else
                        lastNo = (int)linq.Max();
                }
                bool isUpdateFinished = latestNo > lastNo;

                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        Status = "작업이 취소되었습니다.";
                        taskList.Remove(task);
                        return;
                    }
                    int indexOfTasks = taskList.IndexOf(task);
                    if (indexOfTasks > 0 && isUpdateFinished)
                        Status = $"다운로드 대기중..({taskList.IndexOf(task)})";
                    if (indexOfTasks > 0 && !isUpdateFinished)
                        Status = $"URL 캐시 업데이트 대기중..({taskList.IndexOf(task)})";
                    else if (indexOfTasks == 0)
                        break;
                    await Task.Delay(100);
                }
                if (!isUpdateFinished)
                    await UpdateAsync(ct, lastNo + 1);
                await DownloadAsync(ct);
                if (ct.IsCancellationRequested)
                {
                    taskList.Remove(task);
                    Cts = null;
                    return;
                }
                Status = "다운로드 완료";
                taskList.Remove(task);
                Cts = null;
                return;
            });
            taskList.Add(task);
        }

        public async Task DownloadAsync(CancellationToken ct)
        {
            var downloadProgress = new DownloadProgress(this);
            await MainWindowViewModel.Model.Downloader.DownloadAsync(Webtoon,
                                                                     (s) => { Status = s; },
                                                                     downloadProgress,
                                                                     ct);
            if (ct.IsCancellationRequested)
                return;
            Status = "다운로드 완료";
            downloadProgress.Finish();
        }

        public void RegisterUpdateTask(List<Task> taskList)
        {
            Task task = null;
            Cts = new CancellationTokenSource();
            CancellationToken ct = Cts.Token;
            task = Task.Run(async () =>
            {
                NaverWebtoonClient client = new NaverWebtoonClient();
                int latestNo = await client.GetLatestEpisodeNoAsync((int)Webtoon.ID);
                int lastNo;
                using (var context = new WebtoonDbContext())
                {
                    var linq = from e in context.Episodes
                               where e.WebtoonID == Webtoon.ID
                               select e.No;
                    if (!linq.Any())
                        lastNo = 0;
                    else
                        lastNo = (int)linq.Max();
                }
                if (lastNo == latestNo)
                {
                    Status = "URL캐시 업데이트 완료";
                    taskList.Remove(task);
                    return;
                }

                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        Status = "작업이 취소되었습니다.";
                        taskList.Remove(task);
                        return;
                    }
                    int indexOfTasks = taskList.IndexOf(task);
                    if (indexOfTasks > 0)
                    {
                        Status = $"URL캐시 업데이트 대기중..({taskList.IndexOf(task)})";
                        await Task.Delay(300);
                    }
                    else if(indexOfTasks == 0)
                        break;
                    else
                        await Task.Delay(10);
                }

                await UpdateAsync(ct, lastNo + 1);

                taskList.Remove(task);
                Cts = null;
                return;
            });
            taskList.Add(task);
        }

        public async Task UpdateAsync(CancellationToken ct, int from)
        {
            var updateProgress = new UpdateProgress(this);
            await MainWindowViewModel.Model.Downloader.UpdateDbAsync(Webtoon,
                                                                     from,
                                                                     (s) => { Status = s; },
                                                                     updateProgress,
                                                                     ct);
            if (ct.IsCancellationRequested)
                return;
            Status = "URL캐시 업데이트 완료";
            updateProgress.Finish();
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