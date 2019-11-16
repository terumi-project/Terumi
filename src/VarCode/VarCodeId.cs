using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Terumi.Binder;

namespace Terumi.VarCode
{
	// this is so we're EXTREMELY explicit about just a regular integer vs an ID used in a vartree or varcodestore or wherever

	public struct VarCodeId : IEquatable<VarCodeId>
	{
		public VarCodeId(int id) => Id = id;

		public int Id { get; }

		public override bool Equals(object obj) => obj is VarCodeId id && Equals(id);
		public bool Equals([AllowNull] VarCodeId other) => Id == other.Id;
		public override int GetHashCode() => HashCode.Combine(Id);

		public static bool operator ==(VarCodeId left, VarCodeId right) => left.Equals(right);
		public static bool operator !=(VarCodeId left, VarCodeId right) => !(left == right);

		public static implicit operator int(VarCodeId varId) => varId.Id;
		public static implicit operator VarCodeId(int id) => new VarCodeId(id);

		public override string ToString() => Id.ToString();
	}
}
