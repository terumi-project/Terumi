using System;
using System.Diagnostics.CodeAnalysis;

namespace Terumi.Lexer
{
	public struct LexerMetadata : IEquatable<LexerMetadata>
	{
		public int Line;
		public int Column;
		public int BinaryOffset;
		public string File;

		public override string ToString() => $"on line {Line}, column {Column} (binary offset {BinaryOffset}) in file {File}.";

		public override bool Equals(object obj) => obj is LexerMetadata metadata && Equals(metadata);

		public bool Equals([AllowNull] LexerMetadata other) => Line == other.Line && Column == other.Column && BinaryOffset == other.BinaryOffset && File == other.File;

		public override int GetHashCode() => HashCode.Combine(Line, Column, BinaryOffset, File);

		public static bool operator ==(LexerMetadata left, LexerMetadata right) => left.Equals(right);

		public static bool operator !=(LexerMetadata left, LexerMetadata right) => !(left == right);
	}
}