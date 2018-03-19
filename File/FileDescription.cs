using System;
using System.Collections.Generic;
using System.Text;

namespace DownloadManager.File
{
    public class FileDescription
    {
        public string FileName { get; set; }
        public string Url { get; set; }
        public bool DownloadCompleted { get; set; }        
    }
}
