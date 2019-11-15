using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Terumi.Ast;
using Terumi.Binder;

namespace Terumi.VarCode
{
	public class VarCodeTranslator
	{
		private int _methodCounter;
		private Dictionary<IMethod, int> _methods = new Dictionary<IMethod, int>();
		private readonly List<VarTree> _varTrees = new List<VarTree>();

		public void Visit(IEnumerable<IBind> binds)
		{
			foreach (var bind in binds)
			{
				if (bind is MethodBind methodBind)
				{
					InsertAt(GetId(methodBind), Visit(methodBind));
				}
			}
		}

		private void InsertAt(int id, VarTree tree)
		{
			while (id >= _varTrees.Count)
			{
				_varTrees.Add(default);
			}

			_varTrees[id] = tree;
		}

		public VarTree Visit(MethodBind methodBind)
		{
			var id = GetId(methodBind);

			var tree = new VarTree();

			var translator = new VarCodeMethodTranslator(tree, GetId, methodBind);
			translator.Visit(methodBind.Statements);

			return tree;
		}

		private int GetId(IMethod method)
		{
			// TODO: return negative IDs if the method is a compiler defined method
			// idk how though LMAO

			int id;

			if (!_methods.TryGetValue(method, out id))
			{
				id = _methodCounter++;
				_methods[method] = id;
			}

			return id;
		}
	}

	public class VarCodeMethodTranslator
	{
		private readonly VarTree _tree;
		private readonly Func<IMethod, int> _methodIdGenerator;
		private readonly MethodBind _method;

		private readonly Dictionary<string, int> _varReferences = new Dictionary<string, int>();
		private readonly Dictionary<string, int> _paramReferences = new Dictionary<string, int>();

		public VarCodeMethodTranslator(VarTree tree, Func<IMethod, int> methodIdGenerator, MethodBind method)
		{
			_tree = tree;
			_methodIdGenerator = methodIdGenerator;
			_method = method;

			for (var i = 0; i < method.Parameters.Count; i++)
			{
				var name = method.Parameters[i].Name;
				var id = tree.GetParameter(i);

				_paramReferences[name] = id;
			}

			_methodIdGenerator = methodIdGenerator;
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
				case IfStatement i: return; // throw new NotImplementedException();

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
					_tree.Execute(_methodIdGenerator(i.CallingMethod), parameters);
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

		public List<int> Visit(List<ICodeExpression> expressions)
		{
			var vars = new List<int>();

			foreach (var expression in expressions)
			{
				vars.Add(Visit(expression));
			}

			return vars;
		}

		public int Visit(ICodeExpression expression)
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
					var id = _tree.Call(_methodIdGenerator(i.CallingMethod), parameters);
					return id;
				}

				default: throw new NotSupportedException();
			}
		}
	}
}
