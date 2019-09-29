using System.Collections.Generic;

using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Tokenizer
{
	public class Tokenizer : IAstNotificationReceiver
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
			_classPattern = new TypeDefinitionPattern(this, TypeDefinitionType.Class, _classMemberPattern);

			_contractMemberPattern = NoPattern<TerumiMember>.IInstance;
			_contractPattern = new TypeDefinitionPattern(this, TypeDefinitionType.Contract, _contractMemberPattern);

			_typeDefinitionPattern = new CoagulatedPattern<TypeDefinition, TypeDefinition, TypeDefinition>(_classPattern, _contractPattern);

			_compilerUnitItem = new CompilerUnitItemPattern(_typeDefinitionPattern);
			_compilerUnit = new CompilerUnitPattern(this, _compilerUnitItem);
		}

		public void AstCreated<T>(ReaderFork<Token> fork, T ast)
		{
			System.Console.WriteLine("ast: " + ast.GetType().FullName);
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