using System.Collections.Generic;

namespace Fumen
{
	/// <summary>
	/// Simple notifier for IObservers.
	/// </summary>
	/// <typeparam name="T">Type of object being observed.</typeparam>
	public class Notifier<T>
	{
		/// <summary>
		/// All observers of this Notifier.
		/// </summary>
		private List<IObserver<T>> Observers = new List<IObserver<T>>();

		/// <summary>
		/// Adds an Observer.
		/// </summary>
		/// <param name="o">Observer.</param>
		public void AddObserver(IObserver<T> o)
		{
			Observers.Add(o);
		}

		/// <summary>
		/// Removes a previously added Observer.
		/// Does no checking to ensure the Observer is tracked prior to removal.
		/// </summary>
		/// <param name="o">Observer.</param>
		public void RemoveObserver(IObserver<T> o)
		{
			Observers.Remove(o);
		}

		/// <summary>
		/// Notify all IObservers of this Notifier of an event.
		/// </summary>
		/// <param name="eventId">String identifier of the event.</param>
		/// <param name="notifier">Object issuing notification. Typically this.</param>
		protected void Notify(string eventId, T notifier)
		{
			foreach(var	observer in Observers)
			{
				observer.OnNotify(eventId, notifier, null);
			}
		}

		/// <summary>
		/// Notify all IObservers of this Notifier of an event.
		/// </summary>
		/// <param name="eventId">String identifier of the event.</param>
		/// <param name="notifier">Object issuing notification. Typically this.</param>
		/// <param name="payload">Arbitrary object payload for notification.</param>
		protected void Notify(string eventId, T notifier, object payload)
		{
			foreach (var observer in Observers)
			{
				observer.OnNotify(eventId, notifier, payload);
			}
		}
	}
}
