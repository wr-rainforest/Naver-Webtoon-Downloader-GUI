using System;
using System.Collections.Generic;
using System.Text;

namespace NaverWebtoonDownloader.GUI
{
    class UpdateProgress : IProgress<(int position, int count)>
    {
        Action<(int position, int count)> action;
        public UpdateProgress(Action<(int position, int count)> action)
        {
            this.action = action;
        }
        public void Report((int position, int count) value)
        {
            action(value);
        }
    }
}
