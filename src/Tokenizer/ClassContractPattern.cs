using System;
using System.Collections.Generic;

using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Tokenizer
{
	// TODO: make code not copy and pasted
	public class TypeDefinitionPattern : IPattern<TypeDefinition>
	{
		private readonly IPattern<string> _identifierPattern;
		private readonly IPattern<TerumiMember> _memberPattern;
		private readonly TypeDefinitionType _type;
		private readonly Keyword _keyword;

		public TypeDefinitionPattern
		(
			TypeDefinitionType type,
			IPattern<string> identifierPattern,
			IPattern<TerumiMember> memberPattern
		)
		{
			_identifierPattern = identifierPattern;
			_memberPattern = memberPattern;
			_type = type;
			_keyword = type.ToKeyword();
		}

		public bool TryParse(ReaderFork<Token> source, out TypeDefinition item)
		{
			if (!(source.TryNextNonWhitespace<KeywordToken>(out var keywordToken)
			&& keywordToken.Keyword == _keyword
			&& _identifierPattern.TryParse(source, out var identifier)
			&& source.TryNextNonWhitespace<CharacterToken>(out var characterToken)
			&& characterToken.Character == '{'))
			{
				item = default;
				return false;
			}

			// EOF
			if (!source.TryPeek(out _))
			{
				item = new TypeDefinition(identifier, _type, Array.Empty<TerumiMember>());
				return true;
			}

			var members = new List<TerumiMember>();

			while (_memberPattern.TryParse(source, out var member))
			{
				members.Add(member);

				// EOF
				if (!source.TryPeek(out _))
				{
					item = default;
					return false;
				}

				if (source.TryPeekNonWhitespace<CharacterToken>(out var lastToken, out var peeked)
				&& lastToken.Character == '}')
				{
					source.Advance(peeked);
					item = new TypeDefinition(identifier, _type, members.ToArray());
					return true;
				}
			}

			item = default;
			return false;
		}
	}
}