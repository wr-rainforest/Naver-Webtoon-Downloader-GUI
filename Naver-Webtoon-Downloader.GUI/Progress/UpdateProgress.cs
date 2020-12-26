using System;
using System.Collections.Generic;
using System.Text;
using NaverWebtoonDownloader.CoreLib.Database;

namespace NaverWebtoonDownloader.GUI
{
    class UpdateProgress : IProgress<Episode>
    {
        DownloadStatusViewModel _model;

        public UpdateProgress(DownloadStatusViewModel model)
        {
            _model = model;
        }
        public void Report(Episode value)
        {
            _model.ImageCount += value.Images.Count;
            _model.LatestEpisodeInfo = $"[{value.Date:yyyy.MM.dd}] {value.Title}";
        }
    }
}
