using System;
using System.Collections.Generic;
using System.Linq;
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
			while (OptimizeUnusedMethodCalls()) ;
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
	}
}
