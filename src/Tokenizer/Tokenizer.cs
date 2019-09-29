using System.Collections.Generic;

using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Tokenizer
{
	public class Tokenizer
	{
		private readonly IPattern<TerumiMember> _classMemberPattern;
		private readonly IPattern<TypeDefinition> _classPattern;
		private readonly IPattern<TerumiMember> _contractMemberPattern;
		private readonly IPattern<TypeDefinition> _contractPattern;
		private readonly IPattern<TypeDefinition> _typeDefinitionPattern;
		private readonly IPattern<CompilerUnitItem> _compilerUnitItem;
		private readonly IPattern<CompilerUnit> _compilerUnit;

		public Tokenizer()
		{
			_classMemberPattern = NoPattern<TerumiMember>.IInstance;
			_classPattern = new TypeDefinitionPattern(TypeDefinitionType.Class, _classMemberPattern);

			_contractMemberPattern = NoPattern<TerumiMember>.IInstance;
			_contractPattern = new TypeDefinitionPattern(TypeDefinitionType.Contract, _contractMemberPattern);

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

			using var fork = head.Fork();
			fork.Commit = true;
			return _compilerUnit.TryParse(fork, out compilerUnit);
		}
	}
}