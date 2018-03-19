using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace DownloadManager.File
{
    public interface IDownloadFile
    {
        string FileName { get;}
        Uri Url { get; }
        void StartDownload();
        int Progress { get; }
        Task<string> GetFilePath();
        event EventHandler Finsih;
        event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        event EventHandler Start;
    }
}
