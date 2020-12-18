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

        public Downloader Downloader { get; set; }

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

        public string LatestEpisode { get; set; }

        public string Status { get; set; }

        public double Progress { get; set; }

        public string ProgressMessage { get; set; }

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
                OnPropertyChanged("SizeText");
            } 
        }

        public string SizeText { get; private set; }

        public bool IsRunning { get; private set; }

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
        #endregion Property

        #region Command
        public ICommand StartCommand { get; set; }

        public ICommand StopCommand { get; set; }

        public ICommand DeleteCommand { get; set; }

        public void Start()
        {
            RegisterUpdateTask(MainWindowViewModel.Tasks);
            RegisterDownloadTask(MainWindowViewModel.Tasks);
        }

        public void Stop()
        {
            Cts.Cancel();
            Cts = null;
        }

        public void Delete()
        {
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
            Task task = null;
            Cts = new CancellationTokenSource();
            CancellationToken ct = Cts.Token;

            task = Task.Run(async () =>
            {
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
                        Status = $"다운로드 대기중..({taskList.IndexOf(task)})";
                        await Task.Delay(300);
                    }
                    else if (indexOfTasks == 0)
                        break;
                    else
                        await Task.Delay(10);
                }

                await DownloadAsync(ct);

                taskList.Remove(task);
                Cts = null;
                return;
            });
            taskList.Add(task);
        }

        public async Task DownloadAsync(CancellationToken ct)
        {
            var downloadProgress = new DownloadProgress(this);
            await Downloader.DownloadAsync(Webtoon,
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

                await UpdateAsync(ct, lastNo);

                taskList.Remove(task);
                Cts = null;
                return;
            });
            taskList.Add(task);
        }

        public async Task UpdateAsync(CancellationToken ct, int lastNo)
        {
            var updateProgress = new UpdateProgress(this);
            await Downloader.UpdateDbAsync(Webtoon,
                                           lastNo,
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