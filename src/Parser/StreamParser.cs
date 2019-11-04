using System.Collections.Generic;

using Terumi.Parser.Expressions;
using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class StreamParser : IAstNotificationReceiver
	{
		private readonly MethodCallParameterGroupPattern _methodCallParameterGroupPattern;
		private readonly MethodCallPattern _methodCallPattern;
		private readonly ReturnExpressionPattern _returnPattern;
		private readonly AccessExpressionPattern _accessPattern;
		private readonly NumericLiteralExpressionPattern _numericPattern;
		private readonly StringLiteralExpressionPattern _stringPattern;
		private readonly ExpressionPattern _expressionPattern;
		private readonly ThisExpressionPattern _thisPattern;
		private readonly ReferenceExpressionPattern _referencePattern;
		private readonly BooleanLiteralExpressionPattern _booleanPattern;

		private readonly IPattern<PackageLevel> _packageLevelPattern;
		private readonly IPattern<ParameterType> _parameterTypePattern;
		private readonly IPattern<ParameterGroup> _parameterGroupPattern;
		private readonly IPattern<CodeBody> _codeBodyPattern;
		private readonly IPattern<Method> _methodPattern;
		private readonly IPattern<TypeDefinition> _topLevelMethodPattern;
		private readonly IPattern<TypeDefinition> _typeDefinitionPattern;
		private readonly IPattern<CompilerUnitItem> _compilerUnitItem;
		private readonly IPattern<CompilerUnit> _compilerUnit;

		public StreamParser()
		{
			// expressions oh no
			_methodCallParameterGroupPattern = new MethodCallParameterGroupPattern(this);
			_methodCallPattern = new MethodCallPattern(this, _methodCallParameterGroupPattern);
			_returnPattern = new ReturnExpressionPattern(this);
			_accessPattern = new AccessExpressionPattern(this);
			_numericPattern = new NumericLiteralExpressionPattern(this);
			_stringPattern = new StringLiteralExpressionPattern(this);
			_thisPattern = new ThisExpressionPattern(this);
			_referencePattern = new ReferenceExpressionPattern(this);
			_booleanPattern = new BooleanLiteralExpressionPattern(this);

			_expressionPattern = new ExpressionPattern
			(
				_methodCallPattern,
				_returnPattern,
				_accessPattern,
				_numericPattern,
				_stringPattern,
				_thisPattern,
				_referencePattern,
				_booleanPattern
			);

			_methodCallParameterGroupPattern.ExpressionPattern = _expressionPattern;
			_returnPattern.ExpressionPattern = _expressionPattern;
			_accessPattern.ExpressionPattern = _expressionPattern;

			// then other code stuff

			_packageLevelPattern = new PackageLevelPattern(this);

			_parameterTypePattern = new ParameterTypePattern(this);
			_parameterGroupPattern = new ParameterGroupPattern(this, _parameterTypePattern);
			_codeBodyPattern = new CodeBodyPattern(this, _expressionPattern);
			_methodPattern = new MethodPattern(this, _parameterGroupPattern, _codeBodyPattern);

			_topLevelMethodPattern = new TopLevelMethodPattern(this, _methodPattern);

			_typeDefinitionPattern = _topLevelMethodPattern;

			_compilerUnitItem = new CompilerUnitItemPattern(_typeDefinitionPattern, _packageLevelPattern);
			_compilerUnit = new CompilerUnitPattern(this, _compilerUnitItem);
		}

		public void AstCreated<T>(ReaderFork<Token> fork, T ast)
		{
			System.Console.WriteLine("ast: " + ast.GetType().FullName);
		}

		public void DebugPrint(ReaderFork<Token> fork)
		{
#if DEBUG
			using var tmp = fork.Fork();
			for (var i = 0; i < 5 && tmp.TryNext(out var tkn); i++)
			{
				System.Console.WriteLine("debug print - tkn " + tkn.GetType().FullName + " - " + tkn.ToString());
				int c = 1; // for debug breakpoint
			}
#endif
		}

		public void Throw(string message)
		{
			System.Console.WriteLine("AST got 'Throw': " + message);
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