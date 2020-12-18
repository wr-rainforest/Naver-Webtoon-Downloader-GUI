using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    class DownloadStatusViewModel : INotifyPropertyChanged
    {
        public Webtoon Webtoon { get; }

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

        public string Size { get; set; }
        #endregion Property

        #region Command
        public ICommand Start { get; set; }

        public ICommand Stop { get; set; }

        public ICommand Delete { get; set; }
        #endregion

        public DownloadStatusViewModel(Webtoon webtoon)
        {
            Webtoon = webtoon;
            OnPropertyChanged("Title");
            OnPropertyChanged("Writer");
        }

        public void RegisterDownloadTask(List<Task> taskList)
        {
            Task task = null;
            task = Task.Run(async () =>
            {
                await Task.Delay(100);
                while (true)
                {
                    int indexOfTasks = taskList.IndexOf(task);
                    if (indexOfTasks > 0)
                    {
                        Status = $"다운로드 대기중..({taskList.IndexOf(task)})";
                        await Task.WhenAny(taskList);
                    }
                    else if (indexOfTasks == 0)
                        break;
                    else
                        await Task.Delay(10);
                }
                Status = "다운로드중..";
                taskList.Remove(task);
            });
            taskList.Add(task);
        }

        public void RegisterUpdateTask(List<Task> taskList)
        {
            Task task = null;
            task = Task.Run(async () =>
            {
                while (true)
                {
                    int indexOfTasks = taskList.IndexOf(task);
                    if (indexOfTasks > 0)
                    {
                        Status = $"URL캐시 업데이트 대기중..({taskList.IndexOf(task)})";
                        await Task.WhenAny(taskList);
                    }
                    else if(indexOfTasks == 0)
                        break;
                    else
                        await Task.Delay(10);
                }
                Status = "URL캐시 업데이트중..";
                taskList.Remove(task);
            });
            taskList.Add(task);
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