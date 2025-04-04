using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fumen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FumenTests;

[TestClass]
public class TestWorkQueue
{
	private class WorkQueueObserver : IObserver<WorkQueue>
	{
		public enum Action
		{
			Start,
			Finish,
		}

		public readonly List<Action> Actions = [];

		public void OnNotify(string eventId, WorkQueue notifier, object payload)
		{
			switch (eventId)
			{
				case WorkQueue.NotificationWorking:
					Actions.Add(Action.Start);
					break;
				case WorkQueue.NotificationWorkComplete:
					Actions.Add(Action.Finish);
					break;
			}
		}
	}

	[TestMethod]
	public void TestActionRunsSynchronously()
	{
		const int workDelay = 100;

		var wq = new WorkQueue();
		wq.Enqueue(() => { Thread.Sleep(workDelay); });
		wq.Update();
		Assert.IsTrue(wq.IsEmpty());
	}

	[TestMethod]
	public void TestAsyncWaits()
	{
		const int workDelay = 100;
		const int pollDelay = 10;

		var wq = new WorkQueue();
		wq.Enqueue(async () => { await Task.Delay(workDelay); });
		var numSleeps = 0;
		while (true)
		{
			wq.Update();
			if (wq.IsEmpty())
				break;
			numSleeps++;
			Thread.Sleep(pollDelay);
		}

		Assert.IsTrue(numSleeps > workDelay / pollDelay - 4);
	}

	[TestMethod]
	public void TestSequentialOperations()
	{
		const int workDelay = 100;
		const int pollDelay = 10;

		var results = new List<string>();

		var wq = new WorkQueue();
		wq.Enqueue(() => results.Add("A"));
		wq.Enqueue(async () =>
		{
			await Task.Delay(workDelay);
			results.Add("B");
		});
		wq.Enqueue(() => results.Add("C"));
		wq.Enqueue(new Task(() =>
		{
			Thread.Sleep(workDelay);
			results.Add("D");
		}));
		wq.Enqueue(() => results.Add("E"));

		while (true)
		{
			wq.Update();
			if (wq.IsEmpty())
				break;
			Thread.Sleep(pollDelay);
		}

		Assert.AreEqual(5, results.Count);
		Assert.AreEqual("A", results[0]);
		Assert.AreEqual("B", results[1]);
		Assert.AreEqual("C", results[2]);
		Assert.AreEqual("D", results[3]);
		Assert.AreEqual("E", results[4]);
	}

	[TestMethod]
	public void TestNotifications()
	{
		const int workDelay = 100;
		const int pollDelay = 10;

		var wq = new WorkQueue();
		var observer = new WorkQueueObserver();
		wq.AddObserver(observer);

		// Run one action.
		Assert.AreEqual(0, observer.Actions.Count);
		wq.Enqueue(() => { });
		Assert.AreEqual(2, observer.Actions.Count);
		Assert.AreEqual(WorkQueueObserver.Action.Start, observer.Actions[0]);
		Assert.AreEqual(WorkQueueObserver.Action.Finish, observer.Actions[1]);

		// Run more work with async operations so it all runs with no break.
		wq.Enqueue(async () => { await Task.Delay(workDelay); });
		wq.Enqueue(() => { });
		wq.Enqueue(async () => { await Task.Delay(workDelay); });
		wq.Enqueue(() => { });
		while (true)
		{
			wq.Update();
			if (wq.IsEmpty())
				break;
			Thread.Sleep(pollDelay);
		}

		Assert.AreEqual(4, observer.Actions.Count);
		Assert.AreEqual(WorkQueueObserver.Action.Start, observer.Actions[0]);
		Assert.AreEqual(WorkQueueObserver.Action.Finish, observer.Actions[1]);
		Assert.AreEqual(WorkQueueObserver.Action.Start, observer.Actions[2]);
		Assert.AreEqual(WorkQueueObserver.Action.Finish, observer.Actions[3]);
	}

	[TestMethod]
	public void TestCustomCompleteFunction()
	{
		const int workDelay = 100;

		var boxedComplete = new List<bool> { false };
		var wq = new WorkQueue();

		bool IsComplete()
		{
			return boxedComplete[0];
		}

		// Synchronous.
		wq.Enqueue(() => { }, null, IsComplete);
		wq.Update();
		Assert.IsFalse(wq.IsEmpty());
		boxedComplete[0] = true;
		wq.Update();
		Assert.IsTrue(wq.IsEmpty());

		// Asynchronous.
		boxedComplete[0] = false;
		wq.Enqueue(async () => { await Task.Delay(workDelay); }, null, IsComplete);
		var numSleeps = 0;
		while (true)
		{
			wq.Update();
			if (wq.IsEmpty())
				break;
			Thread.Sleep(workDelay);
			numSleeps++;

			if (numSleeps == 5)
			{
				boxedComplete[0] = true;
				wq.Update();
				Assert.IsTrue(wq.IsEmpty());
			}
		}

		Assert.IsTrue(numSleeps == 5);
	}
}
