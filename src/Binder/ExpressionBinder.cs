using System;
using System.Collections.Generic;
using System.Linq;

using Terumi.Ast;
using Terumi.SyntaxTree.Expressions;

namespace Terumi.Binder
{
	public class ExpressionBindingState
	{
		private readonly TypeInformation _typeInformation;
		private readonly UserType? _type;
		private readonly MethodBind _methodBind;

		private readonly List<(string Name, IType Type)> _vars = new List<(string Name, IType Type)>();

		public ExpressionBindingState(TypeInformation typeInformation, UserType? type, MethodBind methodBind)
		{
			_typeInformation = typeInformation;
			_type = type;
			_methodBind = methodBind;
		}

		public ICodeExpression TopLevelBind(IBind entity, Expression expression)
		{
			// things like literals can take this shortcut and become a codeexpr fast
			if (expression is ICodeExpression codeExpression) return codeExpression;

			return expression switch
			{
				MethodCall methodCall => BindMethodCall(entity, methodCall),
				AccessExpression accessExpression => BindAccessExpression(entity, accessExpression),
				ReferenceExpression referenceExpression => BindReferenceExpression(entity, referenceExpression),
				SyntaxTree.Expressions.ThisExpression thisExpression => BindThisExpression(entity, thisExpression),
				VariableExpression variableExpression => BindVariableExpression(entity, variableExpression),
				IfExpression ifExpression => BindIfExpression(entity, ifExpression),
				ReturnExpression returnExpression => new ReturnStatement(TopLevelBind(entity, returnExpression.Expression)),
				_ => throw new Exception("Unparab")
			};
		}

		// method call stuffs:
		public MethodCallExpression BindMethodCall(IBind entity, MethodCall methodCall)
		{
			var methodCallParams = ParseMethodCallParameterGroup(entity, methodCall.Parameters)
				.ToList();

			foreach (var referencable in _typeInformation.AllReferenceableMethods(entity))
			{
				if (referencable.Name == methodCall.MethodName
					&& MethodParametersMatch(referencable.Parameters, methodCallParams))
				{
					return new MethodCallExpression(referencable, methodCallParams);
				}
			}

			Log.Error($"Unable to find matching method call for '{methodCall}' in method {entity}");
			throw new Exception("Binding Exception");
		}

		private IEnumerable<ICodeExpression> ParseMethodCallParameterGroup(IBind entity, MethodCallParameterGroup callParams)
		{
			foreach (var parameter in callParams.Expressions)
			{
				yield return TopLevelBind(entity, parameter);
			}
		}

		private bool MethodParametersMatch(List<ParameterBind> bindParams, List<ICodeExpression> callParams)
		{
			if (bindParams.Count != callParams.Count) return false;

			for (var i = 0; i < bindParams.Count; i++)
			{
				var bindParam = bindParams[i];
				var callParam = callParams[i];

				if (bindParam.Type != callParam.Type) return false;
			}

			return true;
		}

		// access expr:
		private ICodeExpression BindAccessExpression(IBind entity, AccessExpression accessExpression)
		{
			// TODO: get access exprs working
			Log.Warn("Using not working access expressions");
			throw new NotImplementedException();

			var predecessor = TopLevelBind(entity, accessExpression.Predecessor);

			return TopLevelBind(predecessor.Type, accessExpression.Access);
		}

		// ref expr:
		private ICodeExpression BindReferenceExpression(IBind entity, ReferenceExpression referenceExpression)
		{
			// we should first try to reference a variable
			var var = _vars.Find(x => x.Name == referenceExpression.ReferenceName);

			if (var != default)
			{
				return new VariableReferenceExpression(var.Name, var.Type);
			}

			// then try to reference a parameter
			var param = _methodBind.Parameters.Find(x => x.Name == referenceExpression.ReferenceName);

			if (param != default)
			{
				return new ParameterReferenceExpression(param);
			}

			Log.Error($"Unresolved reference '{referenceExpression.ReferenceName}' in {_methodBind}");
			throw new Exception("Binding Exception");
		}

		// this expr:
		private ICodeExpression BindThisExpression(IBind entity, SyntaxTree.Expressions.ThisExpression thisExpression)
		{
			if (_type == null)
			{
				Log.Error($"Attempted to return `this` from within a top level method outside of a class/contract.");
				throw new Exception("Binding Exception");
			}

			return new Ast.ThisExpression(_type);
		}

		// var expr:
		private ICodeExpression BindVariableExpression(IBind entity, VariableExpression variableExpression)
		{
			var value = TopLevelBind(entity, variableExpression.Value);
			var name = variableExpression.Identifier;
			var similar = _vars.Find(x => x.Name == name);

			if (similar != default)
			{
				// if a variable of the same type already exists,
				// make sure the assigment can work

				if (similar.Type != value.Type)
				{
					Log.Error($"Attempt to assign an expression of type {value.Type} to {name}, which is a {similar.Type} type.");
					throw new Exception("Binding Exception");
				}
			}
			else
			{
				// otherwise, create the variable if we can find the type
				if (!_typeInformation.TryGetType(entity, variableExpression.Type.TypeName, out var type))
				{
					Log.Error($"Unable to find type '{variableExpression.Type.TypeName}' for variable '{variableExpression.Identifier}' in {_methodBind}");
					throw new Exception("Binding Exception");
				}

				_vars.Add((variableExpression.Identifier, type));
			}

			return new VariableAssignment(name, value);
		}

		// if expr:
		private IfStatement BindIfExpression(IBind entity, IfExpression ifExpression)
		{
			var comparison = TopLevelBind(entity, ifExpression.Comparison);

			var expressions = new List<CodeStatement>();

			foreach (var expression in ifExpression.True.Expressions)
			{
				var boundExpr = TopLevelBind(entity, expression);

				if (!(boundExpr is CodeStatement statement))
				{
					Log.Error($"Expected CodeStatement while parsing if statement, didn't get one.");
					throw new Exception("Binding Exception");
				}

				expressions.Add(statement);
			}

			return new IfStatement(comparison, expressions);
		}
	}

	public class ExpressionBinder
	{
		private readonly TypeInformation _typeInformation;

		public ExpressionBinder(TypeInformation typeInformation)
			=> _typeInformation = typeInformation;

		public void Bind(MethodBind method)
		{
			if (method.TerumiBacking.Body == null)
			{
				Log.Warn($"Attempting to expression bind a method with no body {method.Name} in {method.Namespace}");
				return;
			}

			var state = new ExpressionBindingState(_typeInformation, null, method);

			foreach (var expression in method.TerumiBacking.Body.Expressions)
			{
				var codeExpression = state.TopLevelBind(method, expression);

				if (codeExpression is CodeStatement statement)
				{
					method.Statements.Add(statement);
				}
				else
				{
					Log.Error($"Couldn't parse expression into statement {method.Name} with expression {expression}");
					return;
				}
			}
		}
	}
}