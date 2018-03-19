using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DownloadManager.File
{
    public class DownloadFile : IDownloadFile
    {
        string fileDescriptionPath;
        FileDescription fileDescription;
        string fileName;
        Uri url;
        int progress;
        WebClient webClient;
        TaskCompletionSource<string> TaskCompletion = new TaskCompletionSource<string>();

        public DownloadFile(string fileName, Uri url)
        {
            this.fileName = fileName;
            fileDescriptionPath = fileName + ".fds";
            this.url = url;

            if (System.IO.File.Exists(fileDescriptionPath))
            {
                var fileDescJson = System.IO.File.ReadAllText(fileDescriptionPath);

                try
                {
                    fileDescription = JsonConvert.DeserializeObject<FileDescription>(fileDescJson);

                    if (fileDescription.FileName != fileName || fileDescription.Url != url.ToString())
                    {
                        new FileDescription() { DownloadCompleted = false, FileName = fileName, Url = url.ToString() };
                        saveDescriptionFile();
                    }
                    else
                    {
                        if (fileDescription.DownloadCompleted)
                            TaskCompletion.SetResult(fileName);
                    }
                }
                catch
                {
                    fileDescription = new FileDescription() { DownloadCompleted = false, FileName = fileName, Url = url.ToString() };
                    saveDescriptionFile();
                }

            }
            else
            {
                fileDescription = new FileDescription() { DownloadCompleted = false, FileName = fileName, Url = url.ToString() };
                saveDescriptionFile();
            }

            webClient = new WebClient();
            webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
            webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnProgressChanged(e.ProgressPercentage);
        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                fileDescription.DownloadCompleted = false;
                saveDescriptionFile();
                TaskCompletion.SetException(e.Error); fileDescription.DownloadCompleted = false;
            }
            else if (e.Cancelled)
            {
                fileDescription.DownloadCompleted = false;
                saveDescriptionFile();
                TaskCompletion.SetException(new Exception("Canceled"));
            }
            else
            {
                fileDescription.DownloadCompleted = true;
                saveDescriptionFile();
                TaskCompletion.SetResult(fileName);
            }

            OnFinish();
        }
        void saveDescriptionFile()
        {
            var json = JsonConvert.SerializeObject(fileDescription);

            System.IO.File.WriteAllText(fileDescriptionPath, json);
        }

        void OnFinish()
        {
            Finsih?.Invoke(this, new EventArgs());
        }

        void OnStart()
        {
            Start?.Invoke(this, new EventArgs());
        }

        void OnProgressChanged(int progress)
        {
            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(progress, null));
        }

        public string FileName => fileName;

        public Uri Url => url;

        public int Progress => progress;

        public bool IsDownloading { get => webClient.IsBusy; }

        public event EventHandler Finsih;
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public event EventHandler Start;

        public void StartDownload()
        {
            if (!fileDescription.DownloadCompleted && !IsDownloading)
                webClient.DownloadFileAsync(url, fileName);
            else
                OnFinish();

        }
        public async Task<string> GetFilePath()
        {
            if (!IsDownloading)
                StartDownload();


            return await TaskCompletion.Task;
        }

    }
}
