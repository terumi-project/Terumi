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

		public LexerMetadata FromConsumed(Span<byte> consumed)
		{
			var from = Copy();
			from.BinaryOffset += consumed.Length;

			for (var i = 0; i < consumed.Length; i++)
			{
				var current = consumed[i];

				if (current == '\n')
				{
					from.Line++;
					from.Column = 1;
				}
				else if (current != '\r')
				{
					from.Column++;
				}
			}

			return from;
		}

		public LexerMetadata Copy() => new LexerMetadata
		{
			Line = Line,
			Column = Column,
			BinaryOffset = BinaryOffset,
			File = File
		};

		public override string ToString() => $"on line {Line}, column {Column} (binary offset {BinaryOffset}) in file {File}.";

		public override bool Equals(object obj) => obj is LexerMetadata metadata && Equals(metadata);

		public bool Equals([AllowNull] LexerMetadata other) => Line == other.Line && Column == other.Column && BinaryOffset == other.BinaryOffset && File == other.File;

		public override int GetHashCode() => HashCode.Combine(Line, Column, BinaryOffset, File);

		public static bool operator ==(LexerMetadata left, LexerMetadata right) => left.Equals(right);

		public static bool operator !=(LexerMetadata left, LexerMetadata right) => !(left == right);
	}
}