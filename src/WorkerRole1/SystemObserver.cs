using System;
using System.Diagnostics;

namespace MassTransit.AzureWorker
{
	public static class SystemObserver
	{
		private static IObserver<ISystemEvent>[] _observers = new IObserver<ISystemEvent>[0];


		public static IObserver<ISystemEvent>[] Swap(params IObserver<ISystemEvent>[] swap)
		{
			var old = _observers;
			_observers = swap;
			return old;
		}

		public static void Notify(ISystemEvent @event)
		{
			foreach (var observer in _observers)
			{
				try
				{
					observer.OnNext(@event);
				}
				catch (Exception ex)
				{
					var message = string.Format("Observer {0} failed with {1}", observer, ex);
					Trace.WriteLine(message);
				}
			}
		}


		public static void Complete()
		{
			foreach (var observer in _observers)
			{
				observer.OnCompleted();
			}
		}
	}
}