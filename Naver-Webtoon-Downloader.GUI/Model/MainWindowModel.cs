using NaverWebtoonDownloader.CoreLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NaverWebtoonDownloader.GUI
{
    class MainWindowModel
    {
        public Config Config { get; set; }

        public Downloader Downloader { get; }

        public NaverWebtoonClient Client { get; }

        public MainWindowModel(Config config)
        {
            Downloader = new Downloader(Config);
            Client = new NaverWebtoonClient();
        }

    }
}
