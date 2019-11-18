using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Terumi.Ast;
using Terumi.Binder;
using Terumi.Targets;
using Terumi.VarCode.Optimizer.Alpha;

namespace Terumi.VarCode.Optimizer.Alpha
{
	public class VarCodeTranslator
	{
		private readonly MethodBind _entry;
		private readonly ICompilerTarget _target;

		public VarCodeTranslator(MethodBind entry, ICompilerTarget target)
		{
			_entry = entry;
			_target = target;
			Store = new VarCodeStore(_entry, _target);
		}

		public VarCodeStore Store { get; }

		public void Visit(IEnumerable<IBind> binds)
		{
			foreach (var bind in binds)
			{
				if (bind is IMethod method)
				{
					var rent = Store.Rent(method);

					if (rent != null)
					{
						Visit(rent);
					}
				}
			}
		}

		public static void Visit(VarCodeStructure structure)
		{
			var translator = new VarCodeMethodTranslator(structure);
			translator.Visit(structure.MethodBind.Statements);
		}
	}

	public class VarCodeMethodTranslator
	{
		private readonly Dictionary<string, VarCodeId> _varReferences = new Dictionary<string, VarCodeId>();
		private readonly Dictionary<string, VarCodeId> _paramReferences = new Dictionary<string, VarCodeId>();
		private readonly VarCodeStructure _structure;
		private int _counter;

		private VarTree _tree => _structure.Tree;

		public VarCodeMethodTranslator(VarCodeStructure structure)
		{
			_structure = structure;

			var method = _structure.MethodBind;
			var tree = _structure.Tree;

			for (var i = 0; i < method.Parameters.Count; i++)
			{
				var name = method.Parameters[i].Name;
				var id = tree.GetParameter(i);

				_paramReferences[name] = id;
			}
		}

		public void Visit(List<CodeStatement> statements)
		{
			foreach (var statement in statements)
			{
				Visit(statement);
			}
		}

		public void Visit(CodeStatement statement)
		{
			switch (statement)
			{
				case IfStatement i:
				{
					if (i.FalseStatements != null)
					{
						// we implement else clauses as separate not comparisons
						// very efficient :^) (/s)
						// TODO: implement better

						var comparisonName = $"__compiler_comparison_{_counter++}";
						var notName = $"__compiler_not_{_counter++}";

						var comparison = new VariableAssignment(comparisonName, i.Comparison);
						var comparisonReference = new VariableReferenceExpression(comparison.VariableName, comparison.Type);

						Visit(statement: comparison);

						_tree.BeginIf(_varReferences[comparisonName]);
						Visit(i.Statements);
						_tree.EndIf();

						var notMethod = _structure.Store.Target.Operator(CompilerOperators.Not, i.Comparison.Type);
						var notOp = new MethodCallExpression(notMethod, new List<ICodeExpression> { comparisonReference });
						var notVar = new VariableAssignment(notName, notOp);

						Visit(statement: notVar);

						_tree.BeginIf(_varReferences[notName]);
						Visit(i.FalseStatements);
						_tree.EndIf();
					}
					else
					{
						_tree.BeginIf(Visit(i.Comparison));
						Visit(i.Statements);
						_tree.EndIf();
					}
				}
				break;

				case VariableAssignment i:
				{
					var name = i.VariableName;
					var id = Visit(i.Value);

					_varReferences[name] = id;
				}
				break;

				case MethodCallExpression i:
				{
					var parameters = Visit(i.Parameters);

					var id = _structure.Store.Id(i.CallingMethod);
					_tree.Execute(id, parameters);
				}
				break;

				case ReturnStatement i:
				{
					_tree.Return(Visit(i.ReturnOn));
				}
				break;

				default: throw new NotImplementedException();
			}
		}

		public List<VarCodeId> Visit(List<ICodeExpression> expressions)
		{
			var vars = new List<VarCodeId>();

			foreach (var expression in expressions)
			{
				vars.Add(Visit(expression));
			}

			return vars;
		}

		public VarCodeId Visit(ICodeExpression expression)
		{
			switch (expression)
			{
				case ConstantLiteralExpression<string> i: return _tree.Push(i.Value);
				case ConstantLiteralExpression<BigInteger> i: return _tree.Push(i.Value);
				case ConstantLiteralExpression<bool> i: return _tree.Push(i.Value);

				case ParameterReferenceExpression i: return _paramReferences[i.Parameter.Name];
				case VariableReferenceExpression i: return _varReferences[i.VarName];

				case MethodCallExpression i:
				{
					var parameters = Visit(i.Parameters);
					var id = _tree.Call(_structure.Store.Id(i.CallingMethod), parameters);
					return id;
				}

				default: throw new NotSupportedException();
			}
		}
	}
}
