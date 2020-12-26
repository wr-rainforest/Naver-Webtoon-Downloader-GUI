using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NaverWebtoonDownloader.CoreLib;

namespace NaverWebtoonDownloader.GUI
{
    class SettingWindowViewModel : INotifyPropertyChanged
    {
        private Config _config;

        #region Properties
        public string MaxConnections
        {
            get => _config.MaxConnections.ToString();
            set
            {
                if (value == null)
                    _config.MaxConnections = 1;
                if (!byte.TryParse(value, out byte numValue) || numValue < 0)
                {
                    MessageBox_Show_ErrorDialog("1 ~ 256 사이의 유효한 정수값을 입력해 주세요.");
                    return;
                }
                _config.MaxConnections = numValue;
            }
        }
        public string DownloadFolder
        {
            get => _config.DownloadFolder;
            set
            {
                _config.DownloadFolder = value;
                OnPropertyChanged();
            }
        }
        public string WebtoonFolderNameFormat
        {
            get => _config.NameFormat.WebtoonFolderNameFormat;
            set 
            {
                _config.NameFormat.WebtoonFolderNameFormat = value.TrimEnd();
                OnPropertyChanged("WebtoonFolderNameFormatExample");
            }
        }
        public string WebtoonFolderNameFormatExample
        {
            get
            {
                try
                {
                    return string.Format(WebtoonFolderNameFormat, 111111, "웹툰명", "작가명");
                }
                catch
                {
                    return "#N/A";
                }
            }
        }
        public string EpisodeFolderNameFormat
        {
            get => _config.NameFormat.EpisodeFolderNameFormat;
            set
            {
                _config.NameFormat.EpisodeFolderNameFormat = value.TrimEnd();
                OnPropertyChanged("EpisodeFolderNameFormatExample");
            }
        }
        public string EpisodeFolderNameFormatExample
        {
            get
            {
                try
                {
                    return string.Format(EpisodeFolderNameFormat, 111111, 1, "2020.12.26", "웹툰명", "회차 제목", "작가명");
                }
                catch
                {
                    return "#N/A";
                }
            }
        }
        public string ImageFileNameFormat
        {
            get => _config.NameFormat.ImageFileNameFormat;
            set
            {
                _config.NameFormat.ImageFileNameFormat = value;
                OnPropertyChanged("ImageFileNameFormatExample");
            }
        }
        public string ImageFileNameFormatExample
        {
            get
            {
                try
                {
                    return string.Format(ImageFileNameFormat, 111111, 1, 1, "웹툰명", "회차 제목", "2020.12.26");
                }
                catch
                {
                    return "#N/A";
                }
            }
        }
        #endregion

        public ICommand ResetCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand OpenFolderDialogCommand { get; set; }

        public Action<string> MessageBox_Show_ErrorDialog { get; set; }

        public Func<string, MessageBoxButton, MessageBoxImage, MessageBoxResult> MessageBox_Show { get; set; }

        public SettingWindowViewModel()
        {
            if (File.Exists(GlobalStatic.ConfigFilePath))
            {
                try
                {
                    _config = JsonSerializer.Deserialize<Config>(
                        File.ReadAllText(GlobalStatic.ConfigFilePath));
                }
                catch (Exception e)
                {
                    throw new FileLoadException("설정 파일 로딩에 실패하였습니다.", e);
                }
            }
            else
            {
                _config = new Config();

            }
            SaveCommand = new AsyncCommand(x => SaveAsync());
            ResetCommand = new AsyncCommand(x => ResetAsync());
            OpenFolderDialogCommand = new Command(x => OpenFolderDialog());
        }

        public async Task SaveAsync()
        {
            try { string.Format(WebtoonFolderNameFormat, 111111, "웹툰명", "작가명"); }
            catch { MessageBox_Show_ErrorDialog("웹툰 폴더명 포맷이 올바르지 않습니다."); return; }
            try { string.Format(EpisodeFolderNameFormat, 111111, 1, "2020.12.26", "웹툰명", "회차 제목", "작가명"); }
            catch { MessageBox_Show_ErrorDialog("회차 폴더명 포맷이 올바르지 않습니다."); return; }
            try { string.Format(ImageFileNameFormat, 111111, 1, 1, "웹툰명", "회차 제목", "2020.12.26"); }
            catch { MessageBox_Show_ErrorDialog("이미지 파일명 포맷이 올바르지 않습니다."); return; }
            await File.WriteAllTextAsync(
                GlobalStatic.ConfigFilePath,
                JsonSerializer.Serialize(_config, new JsonSerializerOptions() { WriteIndented = true }));
        }

        public async Task ResetAsync()
        {
            _config = new Config();
            await File.WriteAllTextAsync(
                GlobalStatic.ConfigFilePath,
                JsonSerializer.Serialize(_config, new JsonSerializerOptions() { WriteIndented = true }));
            OnPropertyChanged("MaxConnections");
            OnPropertyChanged("DownloadFolder");
            OnPropertyChanged("WebtoonFolderNameFormat");
            OnPropertyChanged("WebtoonFolderNameFormatExample");
            OnPropertyChanged("EpisodeFolderNameFormat");
            OnPropertyChanged("EpisodeFolderNameFormatExample");
            OnPropertyChanged("ImageFileNameFormat");
            OnPropertyChanged("ImageFileNameFormatExample");
        }

        public void OpenFolderDialog()
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            dialog.SelectedPath = _config.DownloadFolder;
            if (dialog.ShowDialog() ?? false)
            {
                DownloadFolder = Path.GetFullPath(dialog.SelectedPath);
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion INotifyPropertyChanged
    }
}
