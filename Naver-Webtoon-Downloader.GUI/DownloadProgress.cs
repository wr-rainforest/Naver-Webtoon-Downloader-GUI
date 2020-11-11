using System;
using System.Collections.Generic;
using System.Text;

namespace NaverWebtoonDownloader.GUI
{
    class DownloadProgress : IProgress<object[]>
    {
        Action<object[]> action;
        public DownloadProgress(Action<object[]> action)
        {
            this.action = action;
        }
        public void Report(object[] value)
        {
            action(value);
        }
    }
}
