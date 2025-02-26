﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fumen;

/// <summary>
/// This object offers a thread-safe way to run an asynchronous operation multiple times with
/// the most recent call always running, and cancelling any potentially previously running work
/// so that there is always at most one asynchronous operation running.
/// </summary>
/// <typeparam name="TInput">Type of object used for input state that is passed to the work Task.</typeparam>
/// <typeparam name="TOutput">Type of object used for output from the Task.</typeparam>
public abstract class CancellableTask<TInput, TOutput> where TOutput : class
{
	/// <summary>
	/// CancellationTokenSource used for cancelling a running work Task.
	/// </summary>
	protected CancellationTokenSource CancellationTokenSource;

	/// <summary>
	/// Object to lock for managing multiple calls.
	/// </summary>
	private readonly object Lock = new();

	/// <summary>
	/// The state from the most recent call to Start.
	/// </summary>
	private TInput State;

	/// <summary>
	/// Currently running work Task.
	/// </summary>
	private Task<TOutput> WorkTask;

	/// <summary>
	/// Start a task with the given state.
	/// </summary>
	/// <param name="state">State to provide to the work Task.</param>
	/// <returns>
	/// True if this call performed work and false if this call returned early before doing work
	/// due to multiple calls running simultaneously.
	/// </returns>
	public async Task<TOutput> Start(TInput state)
	{
		var needToAwaitWorkTask = false;
		lock (Lock)
		{
			// Always update the stored state with the most recent call.
			State = state;

			// Check to see if we are already running the work Task from a previous invocation.
			if (CancellationTokenSource != null)
			{
				// If we are already running, and another call has already started the cancellation,
				// then return. That other call will proceed with the update State from above.
				if (CancellationTokenSource.IsCancellationRequested)
					return null;

				// This is the first call to Start while the current work Task is running.
				// We should cancel it and wait for it to cancel, then proceed with a new
				// work Task.
				CancellationTokenSource?.Cancel();
				needToAwaitWorkTask = true;
			}
		}

		// We have cancelled a previously running WorkTask above.
		// We need to wait for it to finish cancelling so we can start a new one.
		if (needToAwaitWorkTask)
		{
			_ = await WorkTask;
		}

		// We are now ready to start the work Task.
		lock (Lock)
		{
			// There is still a case where the CancellationTokenSource might not be null, indicating
			// another task is running. That case is if right before this lock scope was entered,
			// another thread called Start and made it into this scope first. In that case it is
			// safe for this call to return as the other will do the work.
			if (CancellationTokenSource != null)
				return null;

			// Start a new work Task with a new CancellationTokenSource.
			CancellationTokenSource = new CancellationTokenSource();
			var localState = State;
			WorkTask = Task.Run(async () =>
			{
				TOutput result;
				try
				{
					result = await DoWork(localState);
				}
				catch (OperationCanceledException)
				{
					Cancel();
					result = null;
				}
				finally
				{
					lock (Lock)
					{
						CancellationTokenSource?.Dispose();
						CancellationTokenSource = null;
					}
				}

				return result;
			});
		}

		// Wait for the work Task to complete.
		return await WorkTask;
	}

	/// <summary>
	/// Called once when a work Task is cancelled.
	/// Subclasses should perform any needed cancellation work in this method.
	/// </summary>
	protected abstract void Cancel();

	/// <summary>
	/// Called from a work Task.
	/// Subclasses should perform their work in this method.
	/// </summary>
	/// <param name="state">
	/// The state provided to the most recent call of Start.
	/// </param>
	protected abstract Task<TOutput> DoWork(TInput state);
}
