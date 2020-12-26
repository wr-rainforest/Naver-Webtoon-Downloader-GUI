using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NaverWebtoonDownloader.GUI
{
    class InformationWindowViewModel
    {
        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        public string Information =>
            $"Naver-Webtoon-Downloader-GUI v{version.Major}.{version.Minor}\r\n" +
            $"Source: https://github.com/wr-rainforest/Naver-Webtoon-Downloader\r\n" +
            $"{new string('-', 100)}\r\n" +
            Resources.LICENSE +
            $"{new string('-', 100)}\r\n";

        public string OpenSourceLicense => Resources.OpenSourceLicense;
    }
}
