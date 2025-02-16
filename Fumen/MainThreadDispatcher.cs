using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Fumen;

/// <summary>
/// Class for dispatching actions onto the main thread.
/// Expected Usage:
///  Call any of the public static methods to enqueue actions on the main thread.
///  Call Pump on the main thread as needed.
/// </summary>
public sealed class MainThreadDispatcher
{
	/// <summary>
	/// Enqueued actions to run on the main thread.
	/// </summary>
	private static readonly ConcurrentQueue<Action> Actions = new();

	/// <summary>
	/// Runs a given task and enqueues the given continuation to run later on the main thread.
	/// </summary>
	/// <param name="task">Potentially asynchronous task to run.</param>
	/// <param name="continuation">Continuation from task to be run on the main thread.</param>
	public static async Task RunContinuationOnMainThread(Task task, Action continuation)
	{
		await task;
		Actions.Enqueue(continuation);
	}

	/// <summary>
	/// Runs a given task and enqueues the given continuation to run later on the main thread.
	/// Passes the results of the task into the continuation.
	/// </summary>
	/// <param name="task">Potentially asynchronous task to run.</param>
	/// <param name="continuation">Continuation from task to be run on the main thread.</param>
	public static async Task RunContinuationOnMainThread<T>(Task<T> task, Action<T> continuation)
	{
		var t = await task;
		Actions.Enqueue(() => continuation(t));
	}

	/// <summary>
	/// Runs the given action later on the main thread.
	/// </summary>
	/// <param name="action">Action to run on the main thread.</param>
	public static void RunOnMainThread(Action action)
	{
		Actions.Enqueue(action);
	}

	/// <summary>
	/// Pumps enqueued actions on the main thread.
	/// Assumes that the calling thread is the main thread.
	/// </summary>
	public static void Pump()
	{
		while (Actions.TryDequeue(out var action))
			action();
	}
}
