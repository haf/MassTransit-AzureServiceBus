using System;
using System.Threading;
using System.Threading.Tasks;

namespace MassTransit.AzureWorker
{
	/// <summary>
	/// 	Generic process interface, that is registered in the container and managed by the Engine. It is used internally by the infrastructure.
	/// </summary>
	/// <remarks>
	/// 	You can implement this interface and register it int the container, if you want to add some custom start-up or long-running task (order is not guaranteed).
	/// </remarks>
	public interface IEngineProcess : IDisposable
	{
		/// <summary>
		/// 	Is executed by the engine on initialization phase.
		/// </summary>
		void Initialize();

		/// <summary>
		/// 	Creates and starts a long-running task, given the cancellation token to stop it.
		/// </summary>
		/// <param name="token"> The cancellation token. </param>
		/// <returns> Long-running task instance </returns>
		Task Start(CancellationToken token);
	}
}