using System.Collections.Generic;

namespace Fumen.ChartDefinition
{
	/// <summary>
	/// Miscellaneous extra information for components of a Chart.
	/// Used to store data that is unique to specific file types that doesn't map cleanly to
	/// common parameters of Chart components.
	/// </summary>
	public class Extras
	{
		/// <summary>
		/// Extra data from the source file.
		/// </summary>
		private Dictionary<string, object> SourceExtras;

		/// <summary>
		/// Extra data attached to use in the destination file.
		/// </summary>
		private Dictionary<string, object> DestExtras;

		/// <summary>
		/// Constructor.
		/// </summary>
		public Extras()
		{
		}

		/// <summary>
		/// Copy Constructor.
		/// </summary>
		/// <remarks>
		/// Source and Destination extra values are shallow copied.
		/// </remarks>
		/// <param name="other">Extras to copy from.</param>
		public Extras(Extras other)
		{
			if (other.SourceExtras != null)
			{
				SourceExtras = new Dictionary<string, object>();
				foreach (var entry in other.SourceExtras)
				{
					SourceExtras[entry.Key] = entry.Value;
				}
			}
			if (other.DestExtras != null)
			{
				DestExtras = new Dictionary<string, object>();
				foreach (var entry in other.DestExtras)
				{
					DestExtras[entry.Key] = entry.Value;
				}
			}
		}

		/// <summary>
		/// Add an object to the source extra information.
		/// </summary>
		/// <param name="key">Key to identify this object.</param>
		/// <param name="value">Extra information value.</param>
		/// <param name="overwrite">
		/// If true then overwrite an existing value for this key with no Exception.
		/// If false then throw an ArgumentException when a value exists for this key.
		/// </param>
		public void AddSourceExtra(string key, object value, bool overwrite = false)
		{
			if (SourceExtras == null)
				SourceExtras = new Dictionary<string, object>();
			if (overwrite)
				SourceExtras[key] = value;
			else
				SourceExtras.Add(key, value);
		}

		/// <summary>
		/// Removes an object in the source extra information.
		/// </summary>
		/// <param name="key">Key object to remove.</param>
		public void RemoveSourceExtra(string key)
		{
			SourceExtras?.Remove(key);
		}

		/// <summary>
		/// Add an object to the destination extra information.
		/// </summary>
		/// <param name="key">Key to identify this object.</param>
		/// <param name="value">Extra information value.</param>
		/// <param name="overwrite">
		/// If true then overwrite an existing value for this key with no Exception.
		/// If false then throw an ArgumentException when a value exists for this key.
		/// </param>
		public void AddDestExtra(string key, object value, bool overwrite = false)
		{
			if (DestExtras == null)
				DestExtras = new Dictionary<string, object>();
			if (overwrite)
				DestExtras[key] = value;
			else
				DestExtras.Add(key, value);
		}

		/// <summary>
		/// Gets the extra object for the given key is present.
		/// Checks the destination extras.
		/// if not present and checkSource is true, checks the source extras.
		/// </summary>
		/// <param name="key">Key to identifying object.</param>
		/// <param name="value">Out parameter to hold the value.</param>
		/// <param name="checkSource">
		/// If true then check the source extra information if the value was not found in the
		/// destination extra information.
		/// </param>
		/// <returns>True if the value was found and is of type T, and false otherwise.</returns>
		public bool TryGetExtra<T>(string key, out T value, bool checkSource = false)
		{
			if (DestExtras != null
			    && DestExtras.TryGetValue(key, out var destValueObj) && destValueObj is T destT)
			{
				value = destT;
				return true;
			}

			if (checkSource)
				return TryGetSourceExtra(key, out value);

			value = default;
			return false;
		}

		/// <summary>
		/// Gets the extra object for the given key is present.
		/// Checks the source extras.
		/// </summary>
		/// <param name="key">Key to identifying object.</param>
		/// <param name="value">Out parameter to hold the value.</param>
		/// <returns>True if the value was found and is of type T, and false otherwise.</returns>
		public bool TryGetSourceExtra<T>(string key, out T value)
		{
			if (SourceExtras != null
			    && SourceExtras.TryGetValue(key, out var sourceValueObj)
			    && sourceValueObj is T sourceT)
			{
				value = sourceT;
				return true;
			}

			value = default;
			return false;
		}
	}
}
