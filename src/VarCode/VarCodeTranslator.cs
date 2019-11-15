using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Terumi.Ast;
using Terumi.Binder;
using Terumi.VarCode.Optimizer.Alpha;

namespace Terumi.VarCode
{
	public class VarCodeTranslator
	{
		private readonly MethodBind _entry;

		public VarCodeTranslator(MethodBind entry)
		{
			_entry = entry;
			Store = new VarCodeStore(_entry);
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
					_tree.BeginIf(Visit(i.Comparison));

					Visit(i.Statements);

					_tree.EndIf();
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
