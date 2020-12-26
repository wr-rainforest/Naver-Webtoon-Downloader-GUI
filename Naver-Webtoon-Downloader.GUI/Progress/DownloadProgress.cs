using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using NaverWebtoonDownloader.CoreLib.Database;

namespace NaverWebtoonDownloader.GUI
{
    class DownloadProgress : IProgress<(int Pos, int Count, Image Image)>
    {
        private int initpos;
        private long initsize;
        DownloadStatusViewModel _model;

        public DownloadProgress(DownloadStatusViewModel model)
        {
            _model = model;
            initpos = model.DownloadedImageCount;
            initsize = model.Size;
        }

        public void Report((int Pos, int Count, Image Image) value)
        {
            _model.DownloadedImageCount = value.Pos;
            _model.ImageCount = value.Count;
            _model.Size += value.Image.Size;
        }

        public void Rollback()
        {
            _model.DownloadedImageCount = initpos;
            _model.Size = initsize;
        }

        public void FinalizeStatus()
        {
            _model.DownloadedImageCount = _model.ImageCount;
            using (var context = new WebtoonDbContext())
            {
                var query = from i in context.Images
                            where i.WebtoonID == _model.Webtoon.ID && i.IsDownloaded
                            select i.Size;
                _model.Size = query.Sum();
            }
        }
    }
}
