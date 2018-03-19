using DownloadManager.File;
using System;
using System.Collections.Generic;
using System.Text;

namespace DownloadManager.DownloadQueue
{
    public interface IDownloadQueue
    {
        bool IsPause { get; }
        void Enqueue(IDownloadFile downloadFile);
        void PauseDownload();
        void StartDownload();
    }
}
