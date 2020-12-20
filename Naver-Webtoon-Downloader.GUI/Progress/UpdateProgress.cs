using NaverWebtoonDownloader.CoreLib.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaverWebtoonDownloader.GUI
{
    class UpdateProgress : IProgress<(int Pos, int Count, Episode Episode)>
    {
        DownloadStatusViewModel _viewModel;

        public UpdateProgress(DownloadStatusViewModel downloadStatusViewModel)
        {
            _viewModel = downloadStatusViewModel;
        }

        List<Episode> list = new List<Episode>();

        int _count = 1;

        public void Report((int Pos, int Count, Episode Episode) value)
        {
            _viewModel.LatestEpisode = value.Episode.Title;
            _viewModel.Progress = (double) value.Pos / value.Count;
            _viewModel.ProgressMessage = $"{_viewModel.Progress :P} [{value.Pos}/{value.Count}]";
            list.Add(value.Episode);
            _count = value.Count;
        }

        public void FinalizeStatus()
        {
            var lastEpi = list.OrderBy(x => x.No).Last();
            _viewModel.LatestEpisode = $"[{lastEpi.Date:yyyy.MM.dd}] {lastEpi.Title}";
            _viewModel.Progress = 1d;
            _viewModel.ProgressMessage = $"{_viewModel.Progress:P} [{_count}/{_count}]";
        }
    }
}
