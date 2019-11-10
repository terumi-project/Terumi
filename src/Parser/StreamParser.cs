﻿using System;

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
		private readonly ConstantLiteralExpressionBigIntegerPattern _numericPattern;
		private readonly ConstantLiteralExpressionStringPattern _stringPattern;
		private readonly ExpressionPattern _expressionPattern;
		private readonly ThisExpressionPattern _thisPattern;
		private readonly ReferenceExpressionPattern _referencePattern;
		private readonly ConstantLiteralExpressionBooleanPattern _booleanPattern;
		private readonly VariableExpressionPattern _variablePattern;

		private readonly IPattern<PackageReference> _packageLevelPattern;
		private readonly INewPattern<ParameterType> _parameterTypePattern;
		private readonly IPattern<ParameterGroup> _parameterGroupPattern;
		private readonly IPattern<CodeBody> _codeBodyPattern;
		private readonly IPattern<Method> _methodPattern;
		private readonly IPattern<CompilerUnitItem> _compilerUnitItem;
		private readonly IPattern<CompilerUnit> _compilerUnit;

		public StreamParser()
		{
			_parameterTypePattern = new ParameterTypePattern();
			_packageLevelPattern = new PackageLevelPattern(this);
			_parameterGroupPattern = new ParameterGroupPattern(_parameterTypePattern);

			// expressions oh no
			_methodCallParameterGroupPattern = new MethodCallParameterGroupPattern();
			_methodCallPattern = new MethodCallPattern(_methodCallParameterGroupPattern);
			_returnPattern = new ReturnExpressionPattern();
			_accessPattern = new AccessExpressionPattern();
			_numericPattern = new ConstantLiteralExpressionBigIntegerPattern();
			_stringPattern = new ConstantLiteralExpressionStringPattern();
			_thisPattern = new ThisExpressionPattern();
			_referencePattern = new ReferenceExpressionPattern();
			_booleanPattern = new ConstantLiteralExpressionBooleanPattern();
			_variablePattern = new VariableExpressionPattern(_parameterTypePattern);

			_expressionPattern = new ExpressionPattern
			(
				_methodCallPattern,
				_returnPattern,
				_accessPattern,
				_numericPattern,
				_stringPattern,
				_thisPattern,
				_referencePattern,
				_booleanPattern,
				_variablePattern
			);

			_methodCallParameterGroupPattern.ExpressionPattern = _expressionPattern;
			_returnPattern.ExpressionPattern = _expressionPattern;
			_accessPattern.ExpressionPattern = _expressionPattern;
			_variablePattern.ExpressionPattern = _expressionPattern;

			// then other code stuff

			_codeBodyPattern = new CodeBodyPattern(this, _expressionPattern);
			_methodPattern = new MethodPattern(this, _parameterGroupPattern, _codeBodyPattern);

			_compilerUnitItem = new CompilerUnitItemPattern(_methodPattern, _packageLevelPattern);
			_compilerUnit = new CompilerUnitPattern(this, _compilerUnitItem);
		}

		public void AstCreated<T>(ReaderFork<IToken> fork, T ast)
		{
			// Log.Debug("ast: " + ast.GetType().FullName);
		}

		public void DebugPrint(ReaderFork<IToken> fork)
		{
#if DEBUG
			using var tmp = fork.Fork();
			for (var i = 0; i < 5 && tmp.TryNext(out var tkn); i++)
			{
				Log.Debug("debug print - tkn " + tkn.GetType().FullName + " - " + tkn.ToString());
				int c = 1; // for debug breakpoint
			}
#endif
		}

		public void Throw(string message)
		{
			Log.Warn("AST got 'Throw': " + message);
		}

		public bool TryParse(Memory<IToken> tokens, out CompilerUnit compilerUnit)
		{
			return _compilerUnit.TryParse(new ReaderFork<IToken>(0, tokens, null), out compilerUnit);
		}
	}
}