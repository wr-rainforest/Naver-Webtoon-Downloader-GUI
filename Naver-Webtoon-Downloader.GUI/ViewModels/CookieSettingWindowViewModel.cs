using NaverWebtoonDownloader.CoreLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NaverWebtoonDownloader.GUI
{
    class CookieSettingWindowViewModel : INotifyPropertyChanged
    {
        private string _userID;
        private bool _saveCookieEnabled;
        private string _nid_aut;
        private string _nid_ses;

        public bool IsLogined => NaverWebtoonClient.IsLogined;
        public bool SaveCookieEnabled
        {
            get => _saveCookieEnabled;
            set
            {
                _saveCookieEnabled = value;
                OnPropertyChanged();
            } 
        }
        public string UserID
        {
            get => _userID != null 
                ? _userID
                : "#N/A";
            set
            {
                _userID = value;
                OnPropertyChanged();
            }
        }
        public string NID_AUT
        {
            get => _nid_aut;
            set
            {
                _nid_aut = value;
                OnPropertyChanged();
            }
        }
        public string NID_SES
        {
            get => _nid_ses;
            set
            {
                _nid_ses = value;
                OnPropertyChanged();
            }
        }

        public ICommand SetCookieCommand { get; }

        public CookieSettingWindowViewModel()
        {
            SetCookieCommand = new AsyncCommand(x => SetCookie());
        }

        public Func<string, MessageBoxButton, MessageBoxImage, MessageBoxResult> MessageBox_Show { get; set; }
        public Action<string> MessageBox_Show_ErrorDialog { get; set; }

        public async Task SetCookie()
        {
            string cookiePath = $"{GlobalStatic.AppDataFolderPath}\\cookie.json";

            if (IsLogined && !SaveCookieEnabled && File.Exists(cookiePath))
            {
                File.Delete(cookiePath);
                MessageBox_Show($"저장된 쿠키를 삭제하였습니다.", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (IsLogined && SaveCookieEnabled && !File.Exists(cookiePath))
            {
                var keyValuePairs = new Dictionary<string, string>()
                {
                    {"NID_AUT", NID_AUT },
                    {"NID_SES", NID_SES },
                };
                string json = JsonSerializer.Serialize(keyValuePairs, new JsonSerializerOptions() { WriteIndented = true });
                await File.WriteAllTextAsync(cookiePath, json);
                MessageBox_Show($"쿠기를 성공적으로 저장하였습니다.", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (IsLogined)
                return;

            if (string.IsNullOrEmpty(NID_AUT))
            {
                MessageBox_Show_ErrorDialog("NID_AUT 항목을 입력해 주세요.");
                return;
            }
            if (string.IsNullOrEmpty(NID_SES))
            {
                MessageBox_Show_ErrorDialog("NID_SES 항목을 입력해 주세요.");
                return;
            }

            var userID = await new NaverWebtoonClient().SetCookieAsync(NID_AUT, NID_SES);
            if (userID != null && SaveCookieEnabled)
            {
                UserID = userID;
                var keyValuePairs = new Dictionary<string, string>()
                {
                    {"NID_AUT", NID_AUT },
                    {"NID_SES", NID_SES },
                };
                string json = JsonSerializer.Serialize(keyValuePairs, new JsonSerializerOptions() { WriteIndented = true });
                await File.WriteAllTextAsync(cookiePath, json);
                MessageBox_Show($"쿠키 적용에 성공하였습니다. \r\n네이버 아이디: {userID}", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            if (userID != null && !SaveCookieEnabled)
            {
                UserID = userID;
                MessageBox_Show($"쿠키 적용에 성공하였습니다. \r\n네이버 아이디: {userID}", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox_Show_ErrorDialog("쿠키 적용에 실패하였습니다.");
            }
        }

        public async Task LoadAsync()
        {
            if (File.Exists($"{GlobalStatic.AppDataFolderPath}\\cookie.json"))
            {
                string json = await File.ReadAllTextAsync($"{GlobalStatic.AppDataFolderPath}\\cookie.json");
                var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                var userID = await new NaverWebtoonClient().SetCookieAsync(keyValuePairs["NID_AUT"], keyValuePairs["NID_SES"]);
                if (userID != null)
                {
                    UserID = userID;
                    NID_AUT = keyValuePairs["NID_AUT"];
                    NID_SES = keyValuePairs["NID_SES"];
                }
                else
                {
                    File.Delete($"{GlobalStatic.AppDataFolderPath}\\cookie.json");
                }
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion INotifyPropertyChanged
    }
}
