using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rpc.Util
{
	/// <summary>
	/// A simple thread pool
	/// </summary>
	public class Threader : IDisposable
	{
		public List<ThreadObject> _allThreads = new List<ThreadObject>();
		public Queue<ThreadObject> _idleThreads = new Queue<ThreadObject>();
		public ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();
		/// <summary>
		/// the max thread in pool
		/// </summary>
		public int MaxThread { get;  }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="defaultThreadCount"></param>
		public Threader(int defaultThreadCount, int maxThread = 10)
		{
			for (int i = 0; i < defaultThreadCount; i++)
			{
				var obj = CreateNewObject();
				_allThreads.Add(obj);
				_idleThreads.Enqueue(obj);
			}

			MaxThread = maxThread;
		}

		private ThreadObject CreateNewObject()
		{
			var obj = new ThreadObject();
			obj.Done += (sender, args) =>
			{

				if(_actions.TryDequeue(out var action))
				{
					obj.Start(action);
				}

				lock (_idleThreads)
				{
					_idleThreads.Enqueue(obj);
				}
			};
			return obj;
		}

		/// <summary>
		/// 開始工作
		/// </summary>
		/// <param name="act"></param>
		public void Start(Action act)
		{
			ThreadObject obj = null;
			lock (_idleThreads)
			{
				if(_allThreads.Count==MaxThread)
				{
					//thread pool reached the maximun size, you should wait here
					_actions.Enqueue(act);
				}
				else if (_idleThreads.Count > 0)
				{
					obj = _idleThreads.Dequeue();
				}
				else
				{
					obj = CreateNewObject();
					_allThreads.Add(obj);
				}
			}

			if (obj != null)
			{
				obj.Start(act);
			}
		}

		/// <summary>
		/// 釋放掉所有執行續
		/// </summary>

		public void Dispose()
		{
			foreach (var t in _allThreads)
			{
				t.Dispose();
			}
		}
	}

	/// <summary>
	/// 執行續物件
	/// </summary>
	public class ThreadObject : IDisposable
	{
		private readonly Thread _thread;
		private readonly CancellationTokenSource _canceller = new CancellationTokenSource();
		private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
		private bool _working = false;
		/// <summary>
		/// 上一動錯誤
		/// </summary>
		public Exception LastError;
		private bool _threadStarted = false;
		/// <summary>
		/// 完成事件
		/// </summary>
		public event EventHandler Done;

		/// <summary>
		/// 執行續優先權
		/// </summary>
		public ThreadPriority Priority
		{
			get => _thread.Priority;
			set => _thread.Priority = value;
		}

		/// <summary>
		/// 開啟一個執行續物件
		/// </summary>
		public ThreadObject()
		{
			_thread = new Thread(ThreadJob);
			_thread.Start();
			SpinWait.SpinUntil(() => _threadStarted, 1000);
		}

		private void ThreadJob()
		{
			_threadStarted = true;
			while (true)
			{
				try
				{
					_signal.Wait(_canceller.Token);
					if (_canceller.Token.IsCancellationRequested) break;
					try
					{
						_working = true;
						_act?.Invoke();
						LastError = null;
					}
					catch (Exception e)
					{
						LastError = e;
					}
					finally
					{
						_working = false;
					}

					try
					{
						Done?.Invoke(this, null);
					}
					catch
					{
						//do nothing
					}
				}
				catch
				{
					//cancelled
					break;
				}


			}
		}

		private Action _act;


		/// <summary>
		/// 開始一項工作
		/// </summary>
		/// <param name="act"></param>
		/// <exception cref="Exception"></exception>
		public void Start(Action act)
		{
			if (_working)
				throw new Exception("Thread ready working");
			_act = act;
			_signal.Release(1);
		}


		/// <summary>
		/// dispose
		/// </summary>
		public void Dispose()
		{
			_canceller.Cancel(false);
			_thread.Join();
		}
	}
}
