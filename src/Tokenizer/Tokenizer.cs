using System.Collections.Generic;

using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Tokenizer
{
	public class Tokenizer
	{
		private readonly IPattern<string> _identifierPattern;
		private readonly IPattern<TerumiMember> _classMemberPattern;
		private readonly IPattern<TypeDefinition> _classPattern;
		private readonly IPattern<TerumiMember> _contractMemberPattern;
		private readonly IPattern<TypeDefinition> _contractPattern;
		private readonly IPattern<TypeDefinition> _typeDefinitionPattern;
		private readonly IPattern<CompilerUnitItem> _compilerUnitItem;
		private readonly IPattern<CompilerUnit> _compilerUnit;

		public Tokenizer()
		{
			_identifierPattern = default(IPattern<string>);

			_classMemberPattern = default(IPattern<TerumiMember>);
			_classPattern = new TypeDefinitionPattern(Ast.TypeDefinitionType.Class, _identifierPattern, _classMemberPattern);

			_contractMemberPattern = default(IPattern<TerumiMember>);
			_contractPattern = new TypeDefinitionPattern(Ast.TypeDefinitionType.Contract, _identifierPattern, _contractMemberPattern);

			_typeDefinitionPattern = new CoagulatedPattern<TypeDefinition, TypeDefinition, TypeDefinition>(_classPattern, _contractPattern);

			_compilerUnitItem = new CompilerUnitItemPattern(_typeDefinitionPattern);
			_compilerUnit = new CompilerUnitPattern(_compilerUnitItem);
		}

		public bool TryParse(IEnumerable<Token> tokens, out CompilerUnit compilerUnit)
		{
			using var enumerator = tokens.GetEnumerator();

			var head = new ReaderHead<Token>((amt) =>
			{
				var result = new List<Token>(amt);

				for (var i = 0; i < amt && enumerator.MoveNext(); i++)
				{
					result.Add(enumerator.Current);
				}

				return result.ToArray();
			});

			return _compilerUnit.TryParse(head.Fork(), out compilerUnit);
		}
	}
}