﻿using System.Reflection;

namespace StepManiaEditor
{
	/// <summary>
	/// EditorAction to set a Field or a Property for a value type on an object.
	/// </summary>
	/// <typeparam name="T">
	/// Value type of object field or property.
	/// </typeparam>
	internal sealed class ActionSetObjectFieldOrPropertyValue<T> : EditorAction where T : struct
	{
		private readonly T Value;
		private readonly T PreviousValue;
		private readonly object O;
		private readonly string FieldOrPropertyName;
		private readonly bool IsField;
		private readonly FieldInfo FieldInfo;
		private readonly PropertyInfo PropertyInfo;
		private readonly bool DoesAffectFile;

		/// <summary>
		/// Constructor with a given value to set.
		/// It is assumed that a public instance field or property exists on the object with the given fieldOrPropertyName.
		/// </summary>
		/// <param name="o">Object to modify.</param>
		/// <param name="fieldOrPropertyName">Name of Field or Property on the object to modify.</param>
		/// <param name="value">New value to set.</param>
		public ActionSetObjectFieldOrPropertyValue(object o, string fieldOrPropertyName, T value, bool affectsFile)
		{
			O = o;
			Value = value;
			FieldOrPropertyName = fieldOrPropertyName;

			FieldInfo = O.GetType().GetField(FieldOrPropertyName, BindingFlags.Public | BindingFlags.Instance);
			IsField = FieldInfo != null;
			if (!IsField)
				PropertyInfo = O.GetType().GetProperty(FieldOrPropertyName, BindingFlags.Public | BindingFlags.Instance);

			PreviousValue = IsField ? (T)FieldInfo.GetValue(O) : (T)PropertyInfo.GetValue(O);

			DoesAffectFile = affectsFile;
		}

		/// <summary>
		/// Constructor with a given value and previous value to set.
		/// It is assumed value is a Clone of the value.
		/// It is assumed previousValue is a Clone of the previous value.
		/// It is assumed that a public instance field or property exists on the object with the given fieldOrPropertyName.
		/// </summary>
		/// <param name="o"></param>
		/// <param name="fieldOrPropertyName"></param>
		/// <param name="value"></param>
		/// <param name="previousValue"></param>
		public ActionSetObjectFieldOrPropertyValue(object o, string fieldOrPropertyName, T value, T previousValue, bool affectsFile)
		{
			O = o;
			Value = value;
			FieldOrPropertyName = fieldOrPropertyName;

			FieldInfo = O.GetType().GetField(FieldOrPropertyName, BindingFlags.Public | BindingFlags.Instance);
			IsField = FieldInfo != null;
			if (!IsField)
				PropertyInfo = O.GetType().GetProperty(FieldOrPropertyName, BindingFlags.Public | BindingFlags.Instance);

			PreviousValue = previousValue;

			DoesAffectFile = affectsFile;
		}

		public override bool AffectsFile()
		{
			return DoesAffectFile;
		}

		public override string ToString()
		{
			return $"Set {O.GetType()} {FieldOrPropertyName} '{PreviousValue}' > '{Value}'.";
		}

		public override void Do()
		{
			// Set Value on O.
			if (IsField)
				FieldInfo.SetValue(O, Value);
			else
				PropertyInfo.SetValue(O, Value);
		}

		public override void Undo()
		{
			// Set PreviousValue on O.
			if (IsField)
				FieldInfo.SetValue(O, PreviousValue);
			else
				PropertyInfo.SetValue(O, PreviousValue);
		}
	}
}