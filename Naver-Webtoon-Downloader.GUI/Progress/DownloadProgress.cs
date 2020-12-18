using NaverWebtoonDownloader.CoreLib.Database;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace NaverWebtoonDownloader.GUI
{
    class DownloadProgress : IProgress<(int Pos, int Count, int Size)>
    {
        DownloadStatusViewModel _viewModel;

        int _size;

        int _count = 1;

        public DownloadProgress(DownloadStatusViewModel downloadStatusViewModel)
        {
            _viewModel = downloadStatusViewModel;
            using (var context = new WebtoonDbContext())
            {
                var linq = from i in context.Images
                           where i.WebtoonID == _viewModel.Webtoon.ID
                           select i.Size;
                _size = (int)linq.Sum();
            }
        }

        public void Report((int Pos, int Count, int Size) value)
        {
            _viewModel.Progress = (double)value.Pos / value.Count;
            _viewModel.ProgressMessage = $"{_viewModel.Progress: P} [{value.Pos}/{value.Count}]";
            _viewModel.Size = _size + value.Size;
        }

        public void Finish()
        {
            _viewModel.Progress = 1d;
            _viewModel.ProgressMessage = $"{_viewModel.Progress: P} [{_count}/{_count}]";
        }
    }
}
