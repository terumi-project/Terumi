using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Terumi.Ast;
using Terumi.Binder;

namespace Terumi
{
	public class Optimizer
	{
		private readonly TypeInformation _typeInformation;

		public Optimizer(TypeInformation typeInformation)
		{
			_typeInformation = typeInformation;
		}

		public static void Optimize(TypeInformation typeInformation) => new Optimizer(typeInformation).OptimizeAll();

		public void OptimizeAll()
		{
			while (OptimizeUnusedMethodCalls()
				|| OptimizeConstantReturnExpressions()) ;
		}

		public bool OptimizeUnusedMethodCalls()
		{
			var allCalls = new List<Ast.MethodCallExpression>();

			foreach (var bind in _typeInformation.Binds)
			{
				if (bind is MethodBind methodBind)
				{
					ExploreMethodCalls(allCalls, methodBind.Statements);
				}
			}

			var deletedMethod = false;

			for (var i = 0; i < _typeInformation.Binds.Count; i++)
			{
				var bind = _typeInformation.Binds[i];
				if (bind is MethodBind methodBind)
				{
					// don't optimize out 'main' methods
					if (methodBind.Name == "main") continue;

					// if we don't have any calls to the method, we can omit it
					if (!allCalls.Any(x => x.CallingMethod == methodBind))
					{
						Log.Info($"Deleted method '{methodBind.Name}' as nothing referenced it.");

						deletedMethod = true;
						_typeInformation.Binds.RemoveAt(i);
						i--;
					}
				}
			}

			return deletedMethod;
		}

		private void ExploreMethodCalls(List<Ast.MethodCallExpression> calls, List<Ast.CodeStatement> statements)
		{
			foreach (var statement in statements)
			{
				ExploreMethodCalls(calls, (ICodeExpression)statement);
			}
		}

		private void ExploreMethodCalls(List<Ast.MethodCallExpression> calls, ICodeExpression expression)
		{
			if (expression is MethodCallExpression methodCallExpression)
			{
				calls.Add(methodCallExpression);

				foreach (var param in methodCallExpression.Parameters)
				{
					ExploreMethodCalls(calls, param);
				}

				return;
			}

			switch (expression)
			{
				case VariableAssignment variableAssignment: ExploreMethodCalls(calls, variableAssignment.Value); break;
				case ReturnStatement returnStatement: ExploreMethodCalls(calls, returnStatement.ReturnOn); break;
				case IfStatement ifStatement: ExploreMethodCalls(calls, ifStatement.Comparison); ExploreMethodCalls(calls, ifStatement.Statements); break;
			}
		}

		public bool OptimizeConstantReturnExpressions()
		{
			var inlineableBinds = new List<(MethodBind, ICodeExpression)>();

			for (var i = 0; i < _typeInformation.Binds.Count; i++)
			{
				var method = _typeInformation.Binds[i];

				if (!(method is MethodBind methodBind))
				{
					continue;
				}

				if (methodBind.Statements.Count != 1
					|| !(methodBind.Statements[0] is ReturnStatement returnStatement))
				{
					continue;
				}

				if (returnStatement.ReturnOn is ConstantLiteralExpression<string>
					|| returnStatement.ReturnOn is ConstantLiteralExpression<BigInteger>
					|| returnStatement.ReturnOn is ConstantLiteralExpression<bool>)
				{
					Log.Info($"Discovered {methodBind.Name} suitable for inlining");
					inlineableBinds.Add((methodBind, returnStatement.ReturnOn));
					// leave removing the method up to another optimization
					// might be more performant to just erase it here though
					// _typeInformation.Binds.RemoveAt(i--);
				}
			}

			var inlined = false;

			foreach (var bind in _typeInformation.Binds)
			{
				if (!(bind is MethodBind methodBind))
				{
					continue;
				}

				if (RemoveMethodCalls(inlineableBinds, methodBind.Statements))
				{
					inlined = true;
				}
			}

			return inlined;
		}

		public bool RemoveMethodCalls(List<(MethodBind, ICodeExpression)> inlineableBinds, List<CodeStatement> statements)
		{
			var inlined = false;

			for (var i = 0; i < statements.Count; i++)
			{
				var success = ReplaceMethodCalls(inlineableBinds, (ICodeExpression)statements[i], (replace) =>
				{
					// TODO: give better exception message & ensure this can't be hit
					throw new NotSupportedException();
				});

				inlined = success || inlined;
			}

			return inlined;
		}

		public bool ReplaceMethodCalls(List<(MethodBind, ICodeExpression)> inlineableBinds, List<ICodeExpression> expressions)
		{
			var inlined = false;

			for (var i = 0; i < expressions.Count; i++)
			{
				var success = ReplaceMethodCalls(inlineableBinds, expressions[i], replace => expressions[i] = replace);

				inlined = inlined || success;
			}

			return inlined;
		}

		public bool ReplaceMethodCalls(List<(MethodBind, ICodeExpression)> inlineableBinds, ICodeExpression expression, Action<ICodeExpression> replace)
		{
			// if it's a call, we want to check if it has no computed parameters and then replace it
			if (expression is MethodCallExpression methodCallExpression)
			{
				var method = inlineableBinds.Find(x => x.Item1 == methodCallExpression.CallingMethod);

				var inlined = false;

				if (method.Item1 != default && method.Item2 != default)
				{
					// if the method call has no parameters, we can substitute it easily
					if (methodCallExpression.Parameters.Count == 0)
					{
						replace(method.Item2);
						inlined = true;
					}

					// TODO: check if we can replace it
				}

				// for now, let's investigate the parameters
				var success = ReplaceMethodCalls(inlineableBinds, methodCallExpression.Parameters);
				return success || inlined;
			}

			switch (expression)
			{
				case VariableAssignment variableAssignment:
					return ReplaceMethodCalls(inlineableBinds, variableAssignment.Value, replace => variableAssignment.Value = replace);

				case ReturnStatement returnStatement:
					return ReplaceMethodCalls(inlineableBinds, returnStatement.ReturnOn, replace => returnStatement.ReturnOn = replace);

				case IfStatement ifStatement:
					var success1 = ReplaceMethodCalls(inlineableBinds, ifStatement.Comparison, replace => ifStatement.Comparison = replace);
					var success2 = RemoveMethodCalls(inlineableBinds, ifStatement.Statements);
					return success1 || success2;
			}

			return false;
		}
	}
}
