using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Terumi.Ast;
using Terumi.Binder;

namespace Terumi.VarCode
{
	public class VarCodeTranslation
	{
		public VarCodeTranslation(Dictionary<IMethod, VarCodeId> methods, List<VarTree> trees)
		{
			Methods = methods;
			Trees = trees;
		}

		public Dictionary<IMethod, VarCodeId> Methods { get; }
		public List<VarTree> Trees { get; }
	}

	public class VarCodeTranslator
	{
		private int _methodCounter;
		private Dictionary<IMethod, VarCodeId> _methods = new Dictionary<IMethod, VarCodeId>();
		private readonly List<VarTree> _varTrees = new List<VarTree>();

		public VarCodeTranslation GetTranslation()
			=> new VarCodeTranslation(_methods, _varTrees);

		public void Visit(IEnumerable<IBind> binds)
		{
			// first, insert the 'main' method
			var main = binds.OfType<MethodBind>().First(x => x.Name == "main");

			// that way main will be the 0th method, and thus the root.
			InsertAt(GetId(main), Visit(main));

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

		private VarCodeId GetId(IMethod method)
		{
			// TODO: return negative IDs if the method is a compiler defined method
			// idk how though LMAO

			VarCodeId id;

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
		private readonly Func<IMethod, VarCodeId> _methodIdGenerator;
		private readonly MethodBind _method;

		private readonly Dictionary<string, VarCodeId> _varReferences = new Dictionary<string, VarCodeId>();
		private readonly Dictionary<string, VarCodeId> _paramReferences = new Dictionary<string, VarCodeId>();

		public VarCodeMethodTranslator(VarTree tree, Func<IMethod, VarCodeId> methodIdGenerator, MethodBind method)
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
					var id = _tree.Call(_methodIdGenerator(i.CallingMethod), parameters);
					return id;
				}

				default: throw new NotSupportedException();
			}
		}
	}
}
