using System.Collections.Concurrent;
using System.Threading;

namespace Fumen;

/// <summary>
/// Synchronization Context using a single thread.
/// </summary>
public class SynchronizationContextSingleThread : SynchronizationContext
{
	private readonly Thread Thread;
	private readonly ConcurrentQueue<(SendOrPostCallback, object)> Queue = new();
	private readonly AutoResetEvent WorkAvailable = new(false);
	private bool Running = true;

	public SynchronizationContextSingleThread(Thread thread)
	{
		Thread = thread;
	}

	public override void Post(SendOrPostCallback d, object state)
	{
		Queue.Enqueue((d, state));
		WorkAvailable.Set();
	}

	public override void Send(SendOrPostCallback d, object state)
	{
		if (Thread.CurrentThread == Thread)
		{
			d(state);
		}
		else
		{
			var reset = new ManualResetEventSlim();
			Post(_ =>
			{
				d(state);
				reset.Set();
			}, null);
			reset.Wait();
		}
	}

	public void RunMessagePump()
	{
		while (Running)
		{
			if (Queue.TryDequeue(out var work))
			{
				work.Item1(work.Item2);
			}
			else
			{
				WorkAvailable.WaitOne();
			}
		}
	}

	public void Stop()
	{
		Running = false;
		WorkAvailable.Set();
	}
}
