using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace DownloadManager
{
	public abstract class DataDownloaderManager<T>
	{
		Dictionary<T, Data> UrlFile = new Dictionary<T, Data>();
		Queue<Data> DataQueue = new Queue<Data>();

		protected readonly string _directoryToSave;

		object _maxTasksLock = new object();
		int _maxTasks;
		public int MaxTasks
		{
			get
			{
				lock (_maxTasksLock)
					return _maxTasks;
			}

			set
			{
				lock (_maxTasksLock)
					_maxTasks = value;
                onMaxTaskNumberChange(false);
			}

		}

		object _numTasksRunningLock = new object();
		int _numTasksRunning;

        public bool IsWorking
        {
            get
            {
                lock (_numTasksRunningLock)
                    return _numTasksRunning > 0;
            }
        }

        public event EventHandler WorkStop;
        public void OnWorkStop()
        {
            if (WorkStop != null)
                WorkStop(this, new EventArgs());
        }

		void onMaxTaskNumberChange( bool fromRecursive )
		{
			lock (_numTasksRunningLock)
                lock(DataQueue)
			{
                    if (_numTasksRunning == 0 && DataQueue.Count == 0 && !fromRecursive)
                {
					OnWorkStop();
                    return;
				}
                    
				if (_numTasksRunning > MaxTasks)
					return;
			}

			Data data;
			lock (DataQueue)
			{
				if (DataQueue.Count == 0)
					return;

				data = DataQueue.Dequeue();
			}

			lock (_numTasksRunningLock)
			{
				data.Start();
				_numTasksRunning += 1;
			}

			onMaxTaskNumberChange(true);

		}

		public DataDownloaderManager(string directoryToSave, int maxTasks)
		{
			_directoryToSave = directoryToSave;
			Directory.CreateDirectory(directoryToSave);
			_maxTasks = maxTasks;
		}

		void OnDataCompleted(object o, EventArgs e)
		{
			lock (_numTasksRunningLock)
			{
				_numTasksRunning -= 1;
				onMaxTaskNumberChange(false);
			}

		}

		protected virtual void StartDownloading(string url, T uniqueIdent, string fileName)
		{
			bool exist;

			lock (UrlFile)
				exist = UrlFile.ContainsKey(uniqueIdent);

			if (exist)
				return;

			string cachePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			var finalPath = Path.Combine(_directoryToSave, fileName);

			var data = new Data(url, finalPath);
			data.Completed += OnDataCompleted;

			lock (UrlFile)
				UrlFile.Add(uniqueIdent, data);
			lock (DataQueue)
			{
				DataQueue.Enqueue(data);
				onMaxTaskNumberChange(false);
			}
		}

		protected virtual string GetData(string url, T uniqueIdent, string fileName)
		{
			bool exist;

			lock (UrlFile)
				exist = UrlFile.ContainsKey(uniqueIdent);

			if (!exist)
			{
				StartDownloading(url, uniqueIdent, fileName);
				return GetData(url, uniqueIdent, fileName);
			}

			Data data;
			lock (UrlFile)
				data = UrlFile[uniqueIdent];

			data.Wait();
			return data.FilePath;
		}

		protected virtual int GetProgress(T uniqueIdent)
		{
			lock (UrlFile)
				return UrlFile[uniqueIdent].Progress;
		}

		protected virtual bool IsCompleted(T uniqueIdent)
		{
			lock (UrlFile)
				return UrlFile[uniqueIdent].IsCompleted;
		}

		protected virtual void AddCompletationEvent(T uniqueIdent, EventHandler<Data.CompletedEventArgs> func)
		{
			lock (UrlFile)
				UrlFile[uniqueIdent].Completed += func;
		}

		protected virtual void AddProgressEvent(T uniqueIdent, EventHandler<DownloadProgressChangedEventArgs> func)
		{
			lock (UrlFile)
				UrlFile[uniqueIdent].DownloadProgressChanged += func;
		}

		protected virtual void DeleteFiles()
		{
			foreach (var item in UrlFile)
			{
				item.Value.DeleteFile();
			}
		}

		protected bool ContainsIdent(T uniqueIdent)
		{
			lock (UrlFile)
				return UrlFile.ContainsKey(uniqueIdent);
		}

		public class Data
		{
			WebClient client = null;
			AutoResetEvent resetEvent;

			public event EventHandler<CompletedEventArgs> Completed;
			public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged;

			bool completed = false;
			int progress;
			string filePath;
			string _url;
			public string FilePath
			{
				get
				{
					return filePath;
				}
			}

			public int Progress
			{
				get
				{
					return progress;
				}
			}

			public bool IsCompleted
			{
				get
				{
					return completed;
				}
			}

			public Data(string url, string path)
			{
				filePath = path;
				_url = url;
				resetEvent = new AutoResetEvent(false);
				if (File.Exists(path))
				{
					progress = 100;
					completed = true;
					return;
				}
			}

			public void Start()
			{
				if (completed == true)
				{
					resetEvent.Set();
					if (Completed != null)
						onCompleted(null);

					return;
				}

				client = new WebClient();

				client.DownloadProgressChanged += (sender, e) =>
				{
					progress = e.ProgressPercentage;

					if (DownloadProgressChanged != null)
						DownloadProgressChanged(this, e);
				};

				client.DownloadFileCompleted += (sender, e) =>
				{
					completed = true;
					progress = 100;

					resetEvent.Set();
					if (Completed != null)
						onCompleted(e.Error);

					client.Dispose();
					client = null;
				};

				client.DownloadFileAsync(new Uri(_url), filePath);
			}

			public void Wait()
			{
				if (completed)
					return;

				WaitHandle.WaitAll(new WaitHandle[] { resetEvent });
			}

			public void DeleteFile()
			{
				if (!completed)
					client.CancelAsync();

				if (File.Exists(filePath))
					File.Delete(filePath);
			}

			void onCompleted(Exception e)
			{
				if (Completed != null)
					Completed(this, new CompletedEventArgs(e));
			}

			public class CompletedEventArgs : EventArgs
			{
				Exception e;
				public Exception Exception
				{
					get
					{
						return e;
					}
				}

				public CompletedEventArgs(Exception e)
				{
					this.e = e;
				}

			}

		}

	}


}