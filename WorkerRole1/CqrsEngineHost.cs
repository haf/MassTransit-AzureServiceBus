using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransitQueues.CqrsEngine.Events;

namespace MassTransitQueues.CqrsEngine
{
	public sealed class CqrsEngineHost : IDisposable
	{
		private readonly ICollection<IEngineProcess> _serverProcesses;

		private readonly Stack<IDisposable> _disposables = new Stack<IDisposable>();

		public CqrsEngineHost(ICollection<IEngineProcess> serverProcesses)
		{
			_serverProcesses = serverProcesses;

			foreach (var engineProcess in serverProcesses)
			{
				_disposables.Push(engineProcess);
			}
		}

		public void RunForever()
		{
			var token = new CancellationTokenSource();
			Start(token.Token);
			token.Token.WaitHandle.WaitOne();
		}

		public Task Start(CancellationToken token)
		{
			var tasks = _serverProcesses.Select(p => p.Start(token)).ToArray();

			if (tasks.Length == 0)
			{
				throw new InvalidOperationException(string.Format("There were no instances of '{0}' registered",
				                                                  typeof (IEngineProcess).Name));
			}

			var names =
				_serverProcesses.Select(p => string.Format("{0}({1:X8})", p.GetType().Name, p.GetHashCode())).ToArray();

			SystemObserver.Notify(new EngineStarted(names));

			return Task.Factory.StartNew(() =>
				{
					var watch = Stopwatch.StartNew();
					try
					{
						Task.WaitAll(tasks, token);
					}
					catch (OperationCanceledException)
					{
					}
					SystemObserver.Notify(new EngineStopped(watch.Elapsed));
				});
		}


		internal void Initialize()
		{
			SystemObserver.Notify(new EngineInitializationStarted());
			foreach (var process in _serverProcesses)
			{
				process.Initialize();
			}
			SystemObserver.Notify(new EngineInitialized());
		}


		public void Dispose()
		{
			while (_disposables.Count > 0)
			{
				try
				{
					_disposables.Pop().Dispose();
				}
				catch
				{
				}
			}
		}
	}
}