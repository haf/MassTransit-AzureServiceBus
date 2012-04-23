using System;
using System.Threading.Tasks;
using MassTransit.AzureServiceBus.Util;

namespace MassTransit.Transports.AzureServiceBus.Internal
{
	/// <summary>
	/// Docs: http://blogs.msdn.com/b/pfxteam/archive/2010/11/21/10094564.aspx
	/// </summary>
	internal static class TaskExtensions
	{
		class Unit
		{
		}

		/// <summary>
		/// Doesn't cause exceptions of this type to throw
		/// </summary>
		/// <typeparam name="TException"></typeparam>
		/// <param name="t"></param>
		/// <returns></returns>
		public static Task IgnoreExOf<TException>([NotNull] this Task t)
		{
			if (t == null) throw new ArgumentNullException("t");
			return t.ContinueWith(tt => tt.IsFaulted ? new Task(() => { }) : t);
		}

		public static Task Then([NotNull] this Task first, [NotNull] Func<Task> next)
		{
			if (first == null) throw new ArgumentNullException("first");
			if (next == null) throw new ArgumentNullException("next");

			var tcs = new TaskCompletionSource<Unit>();
			return first.ContinueWith(delegate
				{
					if (first.IsFaulted) tcs.TrySetException(first.Exception.InnerExceptions);
					else if (first.IsCanceled) tcs.TrySetCanceled();
					else
					{
						try
						{
							var t = next();
							if (t == null) tcs.TrySetCanceled();
							else t.ContinueWith(delegate
								{
									if (t.IsFaulted) tcs.TrySetException(t.Exception.InnerExceptions);
									else if (t.IsCanceled) tcs.TrySetCanceled();
									else tcs.TrySetResult(new Unit());
								}, TaskContinuationOptions.ExecuteSynchronously);
						}
						catch (Exception exc) { tcs.TrySetException(exc); }
					}
				}, TaskContinuationOptions.ExecuteSynchronously);
		}

		public static Task Then<T1>(this Task<T1> first, Action<T1> next)
		{
			if (first == null) throw new ArgumentNullException("first");
			if (next == null) throw new ArgumentNullException("next");

			var tcs = new TaskCompletionSource<Unit>();
			first.ContinueWith(delegate
				{
					if (first.IsFaulted) tcs.TrySetException(first.Exception.InnerExceptions);
					else if (first.IsCanceled) tcs.TrySetCanceled();
					else
					{
						try
						{
							next(first.Result);
						}
						catch (Exception exc) { tcs.TrySetException(exc); }
					}
				}, TaskContinuationOptions.ExecuteSynchronously);
			return tcs.Task;
		}
		public static Task Then<T1>(this Task<T1> first, Func<T1, Task> next)
		{
			if (first == null) throw new ArgumentNullException("first");
			if (next == null) throw new ArgumentNullException("next");

			var tcs = new TaskCompletionSource<Unit>();
			first.ContinueWith(delegate
			{
				if (first.IsFaulted) tcs.TrySetException(first.Exception.InnerExceptions);
				else if (first.IsCanceled) tcs.TrySetCanceled();
				else
				{
					try
					{
						var t = next(first.Result);
						if (t == null) tcs.TrySetCanceled();
						else t.ContinueWith(delegate
						{
							if (t.IsFaulted) tcs.TrySetException(t.Exception.InnerExceptions);
							else if (t.IsCanceled) tcs.TrySetCanceled();
							else tcs.TrySetResult(new Unit());
						}, TaskContinuationOptions.ExecuteSynchronously);
					}
					catch (Exception exc) { tcs.TrySetException(exc); }
				}
			}, TaskContinuationOptions.ExecuteSynchronously);
			return tcs.Task;
		}

		public static Task<T2> Then<T1, T2>(this Task<T1> first, Func<T1, T2> next)
		{
			if (first == null) throw new ArgumentNullException("first");
			if (next == null) throw new ArgumentNullException("next");

			var tcs = new TaskCompletionSource<T2>();
			first.ContinueWith(delegate
			{
				if (first.IsFaulted) tcs.TrySetException(first.Exception.InnerExceptions);
				else if (first.IsCanceled) tcs.TrySetCanceled();
				else
				{
					try
					{
						tcs.TrySetResult(next(first.Result));
					}
					catch (Exception exc) { tcs.TrySetException(exc); }
				}
			}, TaskContinuationOptions.ExecuteSynchronously);
			return tcs.Task;
		}

		public static Task<T2> Then<T1, T2>(
			[NotNull] this Task<T1> first,
			[NotNull] Func<T1, Task<T2>> next)
		{
			if (first == null) throw new ArgumentNullException("first");
			if (next == null) throw new ArgumentNullException("next");

			var tcs = new TaskCompletionSource<T2>();
			first.ContinueWith(delegate
				{
					if (first.IsFaulted) tcs.TrySetException(first.Exception.InnerExceptions);
					else if (first.IsCanceled) tcs.TrySetCanceled();
					else
					{
						try
						{
							var t = next(first.Result);
							if (t == null) tcs.TrySetCanceled();
							else t.ContinueWith(delegate
								{
									if (t.IsFaulted) tcs.TrySetException(t.Exception.InnerExceptions);
									else if (t.IsCanceled) tcs.TrySetCanceled();
									else tcs.TrySetResult(t.Result);
								}, TaskContinuationOptions.ExecuteSynchronously);
						}
						catch (Exception exc) { tcs.TrySetException(exc); }
					}
				}, TaskContinuationOptions.ExecuteSynchronously);
			return tcs.Task;
		}

		public static Task Sequence(params Func<Task>[] actions)
		{
			Task last = null;
			foreach (var action in actions)
				last = (last == null) ? Task.Factory.StartNew(action).Unwrap() : last.Then(action);
			return last;
		}
	}
}