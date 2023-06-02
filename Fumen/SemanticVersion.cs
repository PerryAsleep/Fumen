using System;

namespace Fumen
{
	public class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
	{
		private readonly int Major;
		private readonly int Minor;
		private readonly int Patch;

		public SemanticVersion()
		{
			Major = 0;
			Minor = 0;
			Patch = 0;
		}

		public SemanticVersion(int major, int minor, int patch)
		{
			Major = major;
			Minor = minor;
			Patch = patch;
		}

		public int CompareTo(SemanticVersion other)
		{
			var comparison = Major.CompareTo(other.Major);
			if (comparison != 0)
				return comparison;
			comparison = Minor.CompareTo(other.Minor);
			if (comparison != 0)
				return comparison;
			return Patch.CompareTo(other.Patch);
		}

		public static bool operator >(SemanticVersion lhs, SemanticVersion rhs)
		{
			return lhs.CompareTo(rhs) > 0;
		}

		public static bool operator <(SemanticVersion lhs, SemanticVersion rhs)
		{
			return lhs.CompareTo(rhs) < 0;
		}

		public static bool operator >=(SemanticVersion lhs, SemanticVersion rhs)
		{
			return lhs.CompareTo(rhs) >= 0;
		}

		public static bool operator <=(SemanticVersion lhs, SemanticVersion rhs)
		{
			return lhs.CompareTo(rhs) <= 0;
		}

		public static bool operator ==(SemanticVersion lhs, SemanticVersion rhs)
		{
			if (lhs == null && rhs == null)
				return true;
			if (lhs == null || rhs == null)
				return false;
			return lhs.CompareTo(rhs) == 0;
		}

		public static bool operator !=(SemanticVersion lhs, SemanticVersion rhs)
		{
			if (lhs == null && rhs == null)
				return false;
			if (lhs == null || rhs == null)
				return true;
			return lhs.CompareTo(rhs) != 0;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is SemanticVersion otherVersion))
				return false;
			return this == otherVersion;
		}

		public bool Equals(SemanticVersion other)
		{
			return this == other;
		}

		public override int GetHashCode()
		{
			var hash = 17;
			hash = unchecked(hash * 31 + Major.GetHashCode());
			hash = unchecked(hash * 31 + Minor.GetHashCode());
			hash = unchecked(hash * 31 + Patch.GetHashCode());
			return hash;
		}

		public override string ToString()
		{
			return Major + "." + Minor + "." + Patch;
		}
	}
}
