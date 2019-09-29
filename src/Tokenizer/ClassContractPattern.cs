using System;
using System.Collections.Generic;

using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Tokenizer
{
	// TODO: make code not copy and pasted
	public class TypeDefinitionPattern : IPattern<TypeDefinition>
	{
		private readonly IPattern<TerumiMember> _memberPattern;
		private readonly TypeDefinitionType _type;
		private readonly Keyword _keyword;
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public TypeDefinitionPattern
		(
			IAstNotificationReceiver astNotificationReceiver,
			TypeDefinitionType type,
			IPattern<TerumiMember> memberPattern
		)
		{
			_memberPattern = memberPattern;
			_type = type;
			_keyword = type.ToKeyword();
			_astNotificationReceiver = astNotificationReceiver;
		}

		public bool TryParse(ReaderFork<Token> source, out TypeDefinition item)
		{
			if (!(source.TryNextNonWhitespace<KeywordToken>(out var keywordToken)
			&& keywordToken.Keyword == _keyword
			// TODO: ensure there's whitespace after `class`/`contract`
			&& source.TryNextNonWhitespace<IdentifierToken>(out var identifierToken)
			&& identifierToken.IdentifierCase == IdentifierCase.PascalCase
			&& source.TryNextNonWhitespace<CharacterToken>(out var characterToken)
			&& characterToken.Character == '{'))
			{
				item = default;
				return false;
			}

			// EOF
			if (!source.TryPeek(out _))
			{
				item = default;
				return false;
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
			}

			if (source.TryNextNonWhitespace<CharacterToken>(out var lastToken)
			&& lastToken.Character == '}')
			{
				item = new TypeDefinition(identifierToken.Identifier, _type, members.ToArray());
				_astNotificationReceiver.AstCreated(source, item);
				return true;
			}

			item = default;
			return false;
		}
	}
}