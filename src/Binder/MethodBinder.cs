using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.Parser;

namespace Terumi.Binder
{
	// used for var decls & whatever else
	public class Scope
	{
		public void RegisterVariable(Statement.Assignment assignment)
		{
		}

		public Scope DeepClone()
		{
			return null;
		}
	}

	public class MethodBinder
	{
		private readonly TerumiBinder _parent;
		private readonly Class _context;
		private readonly Method _method;
		private Scope _scope;
		private readonly SourceFile _file;

		private List<Scope> _levels = new List<Scope>();
		public void IncreaseScope() { _levels.Add(_scope); _scope = _scope.DeepClone(); }
		public void DecreaseScope() { _scope = _levels[^1]; _levels.RemoveAt(_levels.Count - 1); }

		public MethodBinder(TerumiBinder parent, Class? context, Method method, SourceFile file)
		{
			_parent = parent;
			_context = context;
			_method = method;
			_file = file;
		}

		public CodeBody Finalize() => Handle(_method.FromParser.Code);

		public CodeBody Handle(Parser.CodeBody body)
		{
			var stmts = new List<Statement>();

			foreach (var statement in body.Statements)
			{
				stmts.Add(Handle(statement));
			}

			return new CodeBody(stmts);
		}

		public Statement Handle(Parser.Statement stmt)
		{
			switch (stmt)
			{
				case Parser.Statement.Access o: return Handle(o);
				case Parser.Statement.Assignment o: return Handle(o);
				case Parser.Statement.Command o: return Handle(o);
				case Parser.Statement.For o: return Handle(o);
				case Parser.Statement.If o: return Handle(o);
				case Parser.Statement.Increment o: return Handle(o);
				case Parser.Statement.MethodCall o: return Handle(o);
				case Parser.Statement.Return o: return Handle(o);
				case Parser.Statement.While o: return Handle(o);
				default: throw new NotSupportedException($"{stmt.GetType()}");
			}
		}

		public Statement.Access Handle(Parser.Statement.Access o)
			=> new Statement.Access(o, Handle(o.AccessExpression));

		public Statement.Assignment Handle(Parser.Statement.Assignment o)
		{
			var type = _parent.FindImmediateType(o.Type, _file);
			var assignment = new Statement.Assignment(o, type, o.Name, Handle(o.Value));

			_scope.RegisterVariable(assignment);
			return assignment;
		}

		public Statement.Command Handle(Parser.Statement.Command o)
			=> new Statement.Command(o);

		public Statement.For Handle(Parser.Statement.For o)
		{
			IncreaseScope();

			var init = Handle(o.Declaration);
			var comparison = Handle(o.Comparison);
			var execution = Handle(o.Statements);
			var end = Handle(o.End);

			DecreaseScope();

			return new Statement.For(o, init, comparison, end, execution);
		}

		public Statement.If Handle(Parser.Statement.If o)
		{
			var comparison = Handle(o.Comparison);

			IncreaseScope();
			var trueClause = Handle(o.IfClause);
			DecreaseScope();
			IncreaseScope();
			var falseClause = Handle(o.ElseClause);
			DecreaseScope();

			return new Statement.If(o, comparison, trueClause, falseClause);
		}

		public Statement.Increment Handle(Parser.Statement.Increment o)
			=> new Statement.Increment(o, Handle(o.IncrementExpression));

		public Statement.MethodCall Handle(Parser.Statement.MethodCall o)
			=> new Statement.MethodCall(o, Handle(o.MethodCallExpression));

		public Statement.Return Handle(Parser.Statement.Return o)
			=> new Statement.Return(o, Handle(o.Expression));

		public Statement.While Handle(Parser.Statement.While o)
		{
			Expression comparison;
			CodeBody body;

			if (!o.IsDoWhile)
			{
				comparison = Handle(o.Comparison);

				IncreaseScope();
				body = Handle(o.Statements);
				DecreaseScope();
			}
			else
			{
				IncreaseScope();
				body = Handle(o.Statements);

				comparison = Handle(o.Comparison);
				DecreaseScope();
			}

			return new Statement.While(o, o.IsDoWhile, comparison, body);
		}

		public Expression Handle(Parser.Expression expr)
		{
			switch (expr)
			{
				case Parser.Expression.Access o: return Handle(o);
				case Parser.Expression.Binary o: return Handle(o);
				case Parser.Expression.Constant o: return Handle(o);
				case Parser.Expression.Increment o: return Handle(o);
				case Parser.Expression.MethodCall o: return Handle(o);
				case Parser.Expression.Parenthesized o: return Handle(o);
				case Parser.Expression.Reference o: return Handle(o);
				default: throw new NotSupportedException($"{expr.GetType()}");
			}
		}

		public Expression.Access Handle(Parser.Expression.Access o)
			=> new Expression.Access(o, Handle(o.Left), Handle(o.Right));

		public Expression.Increment Handle(Parser.Expression.Increment o) => null;
		public Expression.MethodCall Handle(Parser.Expression.MethodCall o) => null;
	}
}
