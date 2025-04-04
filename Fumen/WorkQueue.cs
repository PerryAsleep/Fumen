using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fumen;

/// <summary>
/// WorkQueue is a queue of Actions and Tasks that are performed sequentially.
/// Enqueued Actions are run synchronously on the WorkQueue's thread.
/// If there is no asynchronous work in the queue, enqueued Actions are run immediately when enqueued.
/// Enqueued Tasks may be async and are run through the TaskScheduler.
/// Explicitly async Tasks may be provided via Funcs returning Tasks.
/// While enqueued work may run asynchronously WorkQueue itself is not thread safe.
/// Callbacks for enqueued work always occur on the WorkQueue's thread.
/// Expected Usage:
///  Call Enqueue to enqueue an Action, Task, or Func containing work to do.
///  Call Update once per frame.
/// </summary>
public sealed class WorkQueue : Notifier<WorkQueue>
{
	public const string NotificationWorking = "Working";
	public const string NotificationWorkComplete = "WorkComplete";

	/// <summary>
	/// An item of work, wrapping an Action, Task, or Func.
	/// </summary>
	internal sealed class WorkQueueItem
	{
		/// <summary>
		/// Action representing the work to perform. May be null.
		/// </summary>
		private readonly Action Action;

		/// <summary>
		/// Task representing the work to perform. May be null.
		/// </summary>
		private Task Task;

		/// <summary>
		/// Func returning a Task representing the work to perform. May be null.
		/// </summary>
		private readonly Func<Task> Func;

		/// <summary>
		/// Optional callback Action to invoke when complete.
		/// </summary>
		private readonly Action Callback;

		/// <summary>
		/// Optional function to determine whether the work is complete or not.
		/// </summary>
		private readonly Func<bool> IsComplete;

		/// <summary>
		/// Whether or not this item is currently running synchronous work.
		/// </summary>
		public bool IsRunningSynchronousWork { get; private set; }

		public WorkQueueItem(Action action)
		{
			Action = action;
		}

		public WorkQueueItem(Action action, Action callback)
		{
			Action = action;
			Callback = callback;
		}

		public WorkQueueItem(Action action, Func<bool> isComplete)
		{
			Action = action;
			IsComplete = isComplete;
		}

		public WorkQueueItem(Action action, Action callback, Func<bool> isComplete)
		{
			Action = action;
			Callback = callback;
			IsComplete = isComplete;
		}

		public WorkQueueItem(Task task)
		{
			Task = task;
		}

		public WorkQueueItem(Task task, Action callback)
		{
			Task = task;
			Callback = callback;
		}

		public WorkQueueItem(Task task, Func<bool> isComplete)
		{
			Task = task;
			IsComplete = isComplete;
		}

		public WorkQueueItem(Task task, Action callback, Func<bool> isComplete)
		{
			Task = task;
			Callback = callback;
			IsComplete = isComplete;
		}

		public WorkQueueItem(Func<Task> func)
		{
			Func = func;
		}

		public WorkQueueItem(Func<Task> func, Action callback)
		{
			Func = func;
			Callback = callback;
		}

		public WorkQueueItem(Func<Task> func, Func<bool> isComplete)
		{
			Func = func;
			IsComplete = isComplete;
		}

		public WorkQueueItem(Func<Task> func, Action callback, Func<bool> isComplete)
		{
			Func = func;
			Callback = callback;
			IsComplete = isComplete;
		}

		public void Start()
		{
			if (Action != null)
			{
				IsRunningSynchronousWork = true;
				Action();
				IsRunningSynchronousWork = false;
			}
			else
			{
				if (Func != null)
				{
					Task = Func();
				}
				else
				{
					Task?.Start();
				}
			}
		}

		public bool IsDone()
		{
			if (Task?.IsCompleted == false)
				return false;
			if (IsComplete != null)
				return IsComplete();
			return true;
		}

		public void Finish()
		{
			Callback?.Invoke();
		}
	}

	/// <summary>
	/// Queue of WorkQueueItems.
	/// </summary>
	private readonly List<WorkQueueItem> Items = [];

	/// <summary>
	/// The currently active WorkQueueItem.
	/// If this is not null then it has been popped from Items.
	/// </summary>
	private WorkQueueItem ActiveItem;

	/// <summary>
	/// Enqueues the given WorkQueueItem.
	/// Notifies observers if this is the first work in the queue.
	/// Starts an Update to immediately finish the work if possible.
	/// </summary>
	/// <param name="workItem">WorkQueueItem to enqueue.</param>
	private void EnqueueInternal(WorkQueueItem workItem)
	{
		var wasWorking = Items.Count > 0 || ActiveItem != null;

		// Perform the action to enqueue the work.
		Items.Add(workItem);

		// Notify if we have started work.
		if (!wasWorking)
			Notify(NotificationWorking, this);

		// Update to process the enqueued work.
		// This will immediately finish enqueued Actions.
		Update();
	}

	/// <summary>
	/// Enqueues the given work, expressed as an Action, to run synchronously.
	/// The given work will run immediately if no work is in the queue.
	/// The given work will run later if there is work in the queue.
	/// </summary>
	/// <param name="work">Work, expressed as an Action, to enqueue.</param>
	public void Enqueue(Action work)
	{
		EnqueueInternal(new WorkQueueItem(work));
	}

	/// <summary>
	/// Enqueues the given work, expressed as an Action, to run synchronously.
	/// The given work will run immediately if no work is in the queue.
	/// The given work will run later if there is work in the queue.
	/// </summary>
	/// <param name="work">Work, expressed as an Action, to enqueue.</param>
	/// <param name="callback">
	/// Callback action that will be invoked when the given work is complete.
	/// Callbacks are invoked on the WorkQueue's thread.
	/// </param>
	public void Enqueue(Action work, Action callback)
	{
		EnqueueInternal(new WorkQueueItem(work, callback));
	}

	/// <summary>
	/// Enqueues the given work, expressed as an Action, to run synchronously.
	/// The given work will run immediately if no work is in the queue.
	/// The given work will run later if there is work in the queue.
	/// </summary>
	/// <param name="work">Work, expressed as an Action, to enqueue.</param>
	/// <param name="isComplete">
	/// Function to use for considering whether the given work is complete or not.
	/// Only if this function returns true will the given work be considered complete and
	/// the WorkQueue will advance to the next enqueued work.
	/// </param>
	public void Enqueue(Action work, Func<bool> isComplete)
	{
		EnqueueInternal(new WorkQueueItem(work, isComplete));
	}

	/// <summary>
	/// Enqueues the given work, expressed as an Action, to run synchronously.
	/// The given work will run immediately if no work is in the queue.
	/// The given work will run later if there is work in the queue.
	/// </summary>
	/// <param name="work">Work, expressed as an Action, to enqueue.</param>
	/// <param name="callback">
	/// Callback action that will be invoked when the given work is complete.
	/// Callbacks are invoked on the WorkQueue's thread.
	/// </param>
	/// <param name="isComplete">
	/// Function to use for considering whether the given work is complete or not.
	/// Only if this function returns true will the given work be considered complete and
	/// the WorkQueue will advance to the next enqueued work.
	/// </param>
	public void Enqueue(Action work, Action callback, Func<bool> isComplete)
	{
		EnqueueInternal(new WorkQueueItem(work, callback, isComplete));
	}

	/// <summary>
	/// Enqueues the given work, expressed as a Task, to run through the TaskScheduler.
	/// The given work may run asynchronously.
	/// </summary>
	/// <param name="work">Work, expressed as a Task, to enqueue.</param>
	public void Enqueue(Task work)
	{
		EnqueueInternal(new WorkQueueItem(work));
	}

	/// <summary>
	/// Enqueues the given work, expressed as a Task, to run through the TaskScheduler.
	/// The given work may run asynchronously.
	/// </summary>
	/// <param name="work">Work, expressed as a Task, to enqueue.</param>
	/// <param name="callback">
	/// Callback action that will be invoked when the given work is complete.
	/// Callbacks are invoked on the WorkQueue's thread.
	/// </param>
	public void Enqueue(Task work, Action callback)
	{
		EnqueueInternal(new WorkQueueItem(work, callback));
	}

	/// <summary>
	/// Enqueues the given work, expressed as a Task, to run through the TaskScheduler.
	/// The given work may run asynchronously.
	/// </summary>
	/// <param name="work">Work, expressed as a Task, to enqueue.</param>
	/// <param name="isComplete">
	/// Function to use for considering whether the given work is complete or not.
	/// Only if this function returns true will the given work be considered complete and
	/// the WorkQueue will advance to the next enqueued work.
	/// </param>
	public void Enqueue(Task work, Func<bool> isComplete)
	{
		EnqueueInternal(new WorkQueueItem(work, isComplete));
	}

	/// <summary>
	/// Enqueues the given work, expressed as a Task, to run through the TaskScheduler.
	/// The given work may run asynchronously.
	/// </summary>
	/// <param name="work">Work, expressed as a Task, to enqueue.</param>
	/// <param name="callback">
	/// Callback action that will be invoked when the given work is complete.
	/// Callbacks are invoked on the WorkQueue's thread.
	/// </param>
	/// <param name="isComplete">
	/// Function to use for considering whether the given work is complete or not.
	/// Only if this function returns true will the given work be considered complete and
	/// the WorkQueue will advance to the next enqueued work.
	/// </param>
	public void Enqueue(Task work, Action callback, Func<bool> isComplete)
	{
		EnqueueInternal(new WorkQueueItem(work, callback, isComplete));
	}

	/// <summary>
	/// Enqueues the given work, expressed as a Function returning a Task, to run through the TaskScheduler.
	/// This is intended for passing in explicit async work.
	/// The given work may run asynchronously.
	/// </summary>
	/// <param name="func">Work, expressed as a Function returning a Task, to enqueue.</param>
	public void Enqueue(Func<Task> func)
	{
		EnqueueInternal(new WorkQueueItem(func));
	}

	/// <summary>
	/// Enqueues the given work, expressed as a Function returning a Task, to run through the TaskScheduler.
	/// The given work may run asynchronously.
	/// </summary>
	/// <param name="func">Work, expressed as a Function returning a Task, to enqueue.</param>
	/// <param name="callback">
	/// Callback action that will be invoked when the given work is complete.
	/// Callbacks are invoked on the WorkQueue's thread.
	/// </param>
	public void Enqueue(Func<Task> func, Action callback)
	{
		EnqueueInternal(new WorkQueueItem(func, callback));
	}

	/// <summary>
	/// Enqueues the given work, expressed as a Function returning a Task, to run through the TaskScheduler.
	/// The given work may run asynchronously.
	/// </summary>
	/// <param name="func">Work, expressed as a Function returning a Task, to enqueue.</param>
	/// <param name="isComplete">
	/// Function to use for considering whether the given work is complete or not.
	/// Only if this function returns true will the given work be considered complete and
	/// the WorkQueue will advance to the next enqueued work.
	/// </param>
	public void Enqueue(Func<Task> func, Func<bool> isComplete)
	{
		EnqueueInternal(new WorkQueueItem(func, isComplete));
	}

	/// <summary>
	/// Enqueues the given work, expressed as a Function returning a Task, to run through the TaskScheduler.
	/// The given work may run asynchronously.
	/// </summary>
	/// <param name="func">Work, expressed as a Function returning a Task, to enqueue.</param>
	/// <param name="callback">
	/// Callback action that will be invoked when the given work is complete.
	/// Callbacks are invoked on the WorkQueue's thread.
	/// </param>
	/// <param name="isComplete">
	/// Function to use for considering whether the given work is complete or not.
	/// Only if this function returns true will the given work be considered complete and
	/// the WorkQueue will advance to the next enqueued work.
	/// </param>
	public void Enqueue(Func<Task> func, Action callback, Func<bool> isComplete)
	{
		EnqueueInternal(new WorkQueueItem(func, callback, isComplete));
	}

	/// <summary>
	/// Update function to process work in the queue.
	/// </summary>
	public void Update()
	{
		var wasEmpty = IsEmpty();

		if (ActiveItem != null)
		{
			if (!ActiveItem.IsDone())
				return;
			ActiveItem.Finish();
			ActiveItem = null;
		}

		while (true)
		{
			if (Items.Count == 0)
				break;
			ActiveItem = Items[0];
			Items.RemoveAt(0);
			ActiveItem.Start();
			if (!ActiveItem.IsDone())
				break;
			ActiveItem.Finish();
			ActiveItem = null;
		}

		if (!wasEmpty && IsEmpty())
			Notify(NotificationWorkComplete, this);
	}

	/// <summary>
	/// Returns whether or not the WorkQueue is actively running synchronous work.
	/// </summary>
	/// <returns>True if the WorkQueue is actively running synchronous work and false otherwise.</returns>
	public bool IsRunningSynchronousWork()
	{
		return ActiveItem?.IsRunningSynchronousWork == true;
	}

	/// <summary>
	/// Returns whether or not the WorkQueue is empty.
	/// </summary>
	/// <returns>True if the WorkQueue is empty and false otherwise.</returns>
	public bool IsEmpty()
	{
		return ActiveItem == null && Items.Count == 0;
	}
}
