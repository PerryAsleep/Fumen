namespace Fumen
{
	/// <summary>
	/// Simple Observer interface for being notified of changes to a notifying object.
	/// See also Notifier.
	/// </summary>
	/// <typeparam name="T">Type of object being observed for notifications.</typeparam>
	public interface IObserver<in T>
	{
		void OnNotify(string eventId, T notifier, object payload);
	}
}
