using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Terumi.Lexer;

namespace Terumi.Parser
{
	public struct ConsumedTokens
	{
		public ConsumedTokens(List<Token> tokens)
		{
			Tokens = tokens;
		}

		public List<Token> Tokens { get; }
	}

	public class TerumiParser
	{
		private const MethodImplOptions MaxOpt = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
		private readonly List<Token> _tokens;
		private int _i;

		public TerumiParser(List<Token> tokens)
		{
			_tokens = tokens;
			ConsumeWhitespace(false);
		}

		public SourceFile ConsumeSourceFile(PackageLevel defaultLevel)
		{
			var start = Current();
			if (AtEnd()) return new SourceFile(TakeTokens(start, Current()), defaultLevel, EmptyList<PackageLevel>.Instance);

			var packageLevel = defaultLevel;

			if (Peek().Type == TokenType.Package)
			{
				Next();

				ConsumeWhitespace();
				packageLevel = ConsumePackageLevel();

				ConsumeWhitespace();
			}

			var packages = new List<PackageLevel>();

			while (Peek().Type == TokenType.Use)
			{
				Next();
				ConsumeWhitespace();

				packages.Add(ConsumePackageLevel());
				ConsumeWhitespace();
			}

			// TODO: function types and whatnot

			return new SourceFile(TakeTokens(start, Current()), packageLevel, packages);
		}

		public PackageLevel ConsumePackageLevel()
		{
			if (AtEnd()) Unsupported($"Cannot consume a package level at the end of the token list");

			if (Peek().Type != TokenType.IdentifierToken)
			{
				Unsupported($"Expected identifier type");
			}

			var levels = new List<string> { (string)Peek().Data };

			Next();
			while (Peek().Type == TokenType.Dot)
			{
				Next();

				if (Peek().Type != TokenType.IdentifierToken)
				{
					Unsupported($"Expected another identifier in namespace, got {Peek().Type}");
				}

				levels.Add((string)Peek().Data);

				Next();
			}

			return new PackageLevel(levels);
		}

		public bool TryMethod()
		{
			return false;
		}

		/* core */

		private void ConsumeWhitespace(bool mustConsumeWhitespace = true)
		{
			bool didConsume = false;

			while (!AtEnd()
				&& (Peek().Type == TokenType.Whitespace
				|| Peek().Type == TokenType.Comment))
			{
				didConsume = true;
				Next();
			}

			if (mustConsumeWhitespace && !didConsume)
			{
				Unsupported($"Didn't consume necessary whitespace");
			}
		}

		private Token Peek(int amt = 0)
		{
			if (_tokens.Count <= amt + _i) Unsupported("No more tokens to peek from");
			return _tokens[amt + _i];
		}

		private void Next() => _i++;

		public bool AtEnd() => _i >= _tokens.Count - 1;

		private int Current() => _i;

		// TODO: more efficient take routine
		private ConsumedTokens TakeTokens(int start, int end)
			=> new ConsumedTokens(_tokens.Skip(start).Take(end).ToList());

		private void Unsupported(string reason)
		{
			throw new InvalidOperationException(reason);
		}
	}
}
