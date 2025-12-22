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
	/// Main thread id.
	/// </summary>
	private static int? MainThreadId;

	/// <summary>
	/// Whether to run actions immediately if they are being enqueued on the main thread already.
	/// </summary>
	private static bool RunMainThreadActionsImmediatelyIfAlreadyOnMainThread;

	/// <summary>
	/// Sets the main thread id. Necessary if configuring the MainThreadDispatcher to execute
	/// actions enqueued on the main thread immediately.
	/// </summary>
	/// <param name="threadId">The main thread id.</param>
	public static void SetMainThreadId(int threadId)
	{
		MainThreadId = threadId;
	}

	/// <summary>
	/// Configures the MainThreadDispatcher to execute enqueued actions immediately if they are
	/// being enqueued on the main thread already.
	/// </summary>
	/// <param name="runImmediately"></param>
	public static void SetRunMainThreadActionsImmediatelyIfAlreadyOnMainThread(bool runImmediately)
	{
		if (MainThreadId == null && runImmediately)
			throw new InvalidOperationException("SetMainThreadId must be called to before enabling immediate action execution.");
		RunMainThreadActionsImmediatelyIfAlreadyOnMainThread = runImmediately;
	}

	/// <summary>
	/// Returns whether or not the current thread is the main thread.
	/// </summary>
	/// <returns>True if the current thread is the main thread and false otherwise.</returns>
	private static bool IsOnMainThread()
	{
		return MainThreadId == Environment.CurrentManagedThreadId;
	}

	/// <summary>
	/// Runs a given task and enqueues the given continuation to run later on the main thread.
	/// </summary>
	/// <param name="task">Potentially asynchronous task to run.</param>
	/// <param name="continuation">Continuation from task to be run on the main thread.</param>
	public static async Task RunContinuationOnMainThread(Task task, Action continuation)
	{
		await task;
		if (RunMainThreadActionsImmediatelyIfAlreadyOnMainThread && IsOnMainThread())
			continuation();
		else
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
		if (RunMainThreadActionsImmediatelyIfAlreadyOnMainThread && IsOnMainThread())
			continuation(t);
		else
			Actions.Enqueue(() => continuation(t));
	}

	/// <summary>
	/// Runs the given action later on the main thread.
	/// </summary>
	/// <param name="action">Action to run on the main thread.</param>
	public static void RunOnMainThread(Action action)
	{
		if (RunMainThreadActionsImmediatelyIfAlreadyOnMainThread && IsOnMainThread())
			action();
		else
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
