using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DownloadManager.File;

namespace DownloadManager.DownloadQueue
{
    public class DownloadQueue : IDownloadQueue
    {
        Queue<IDownloadFile> downloadFileQueue = new Queue<IDownloadFile>();
        bool isPaused = true;
        bool isDownloading = false;

        public bool IsPause => isPaused;

        public void Enqueue(IDownloadFile downloadFile)
        {
            downloadFileQueue.Enqueue(downloadFile);
            if(downloadFileQueue.Count == 1 && !isDownloading && !isPaused)
            {
                var newOne = downloadFileQueue.Dequeue();
                newOne.Finsih += OnFileFinish;
                newOne.StartDownload();  
            }
        }

        private void OnFileFinish(object sender, EventArgs e)
        {
            var downloadFile = (IDownloadFile)sender;
            downloadFile.Finsih -= OnFileFinish;
            if( !isPaused && downloadFileQueue.Count > 0)
            {
                isDownloading = false;
                download();
            }
        }

        void download()
        {
            //Task.Delay(3000).GetAwaiter().GetResult();
            isDownloading = true;
            var newOne = downloadFileQueue.Dequeue();
            newOne.Finsih += OnFileFinish;
            newOne.StartDownload();
        }

        public void PauseDownload()
        {
            isPaused = true;
        }

        public void StartDownload()
        {
            if (!isPaused)
                return;
            isPaused = false;

            if (downloadFileQueue.Count > 0)
            {
                download();
            }

        }
    }
}
