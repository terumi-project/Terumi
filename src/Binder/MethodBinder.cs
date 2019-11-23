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
		public List<Statement.Assignment> Assignments { get; set; } = new List<Statement.Assignment>();
		public Expression? AccessExpression { get; set; }

		public void RegisterVariable(Statement.Assignment assignment)
			=> Assignments.Add(assignment);

		public Scope Clone()
		{
			var assignmentsCopy = Assignments.Select(x => x).ToList();
			return new Scope { Assignments = assignmentsCopy };
		}
	}

	public class MethodBinder
	{
		private readonly TerumiBinder _parent;
		private readonly Class _context;
		private readonly Method _method;
		private Scope _scope = new Scope();
		private readonly SourceFile _file;

		private List<Scope> _levels = new List<Scope>();
		public void IncreaseScope() { _levels.Add(_scope); _scope = _scope.Clone(); }
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

		// statements

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
			var value = Handle(o.Value);

			if (type == BuiltinType.Void)
			{
				type = value.Type;
			}
			else
			{
				if (type != value.Type)
				{
					throw new InvalidOperationException($"Invalid type mismatch when setting {o} to {value}");
				}
			}

			System.Diagnostics.Debug.Assert(type != BuiltinType.Void, "Assignment cannot result in a void type");
			var assignment = new Statement.Assignment(o, type, o.Name, value);

			_scope.RegisterVariable(assignment);
			return assignment;
		}

		public Statement.Command Handle(Parser.Statement.Command o)
			=> new Statement.Command(o, Convert(o.String));

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

		// expressions
		// TODO: [MAJOR] need to account for access expressions

		public Expression Handle(Parser.Expression expr)
		{
			switch (expr)
			{
				case Parser.Expression.Access o: return Handle(o);
				case Parser.Expression.Binary o: return Handle(o);
				case Parser.Expression.Constant o: return Handle(o);
				case Parser.Expression.Increment o: return Handle(o);
				case Parser.Expression.MethodCall o: return Handle(o);
				case Parser.Expression.New o: return Handle(o);
				case Parser.Expression.Parenthesized o: return Handle(o);
				case Parser.Expression.Reference o: return Handle(o);
				default: throw new NotSupportedException($"{expr.GetType()}");
			}
		}

		public Expression.Access Handle(Parser.Expression.Access o)
		{
			// increase scope specifically for the access expression stuff
			IncreaseScope();
			var access = Handle(o.Left);
			_scope.AccessExpression = access;
			var right = Handle(o.Right);
			DecreaseScope();

			return new Expression.Access(o, access, right);
		}

		// TODO: need to invent not expression lol that's pretty critical
		public Expression.Binary Handle(Parser.Expression.Binary o)
			=> new Expression.Binary(o, Handle(o.Left), o.Operator.ToBinaryExpression(), Handle(o.Right));

		public Expression.Constant Handle(Parser.Expression.Constant o)
		{
			var value = o.Value is Parser.StringData strData ? Convert(strData) : o.Value;
			return new Expression.Constant(o, value);
		}

		public Expression.Increment Handle(Parser.Expression.Increment o)
			=> new Expression.Increment(o, Handle(o.Expression), o.Side.ToIncrementType(o.Type));

		public Expression.MethodCall Handle(Parser.Expression.MethodCall o)
		{
			var exprs = new List<Expression>();

			foreach (var expr in o.Parameters)
			{
				exprs.Add(Handle(expr));
			}

			if (_scope.AccessExpression != null)
			{
				if (!_parent.FindMethod(o.IsCompilerCall, o.Name, exprs, _scope.AccessExpression.Type.Methods, out var accessMethod))
				{
					throw new InvalidOperationException($"Cannot find method '{o}' in access expression '{_scope.AccessExpression}'");
				}

				return new Expression.MethodCall(o, accessMethod, exprs);
			}

			if (!_parent.FindImmediateMethod(o, exprs, out var method))
			{
				throw new InvalidOperationException($"Call to method {o} but couldn't find any immediate methods.");
			}

			return new Expression.MethodCall(o, method, exprs);
		}

		public Expression.New Handle(Parser.Expression.New o)
		{
			var exprs = new List<Expression>();

			foreach (var expr in o.Expressions)
			{
				exprs.Add(Handle(expr));
			}

			var type = _parent.FindImmediateType(o.Type, _file);

			if (!_parent.FindMethod(false, "ctor", exprs, type.Methods, out var ctorMethod))
			{
				throw new InvalidOperationException($"Failed to find constructor method for {o}");
			}

			return new Expression.New(o, type, ctorMethod, exprs);
		}

		public Expression.Parenthesized Handle(Parser.Expression.Parenthesized o)
			=> new Expression.Parenthesized(o, Handle(o.Inner));

		public Expression.Reference Handle(Parser.Expression.Reference o)
		{
			// we gotta check if we're handling an access expression
			if (_scope.AccessExpression != null)
			{
				// if we are, we're going to try to reference the fields of the access expression type
				var fields = _scope.AccessExpression.Type.Fields;

				foreach (var field in fields)
				{
					if (field.Name == o.ReferenceName)
					{
						return new Expression.Reference.Field(o, field);
					}
				}

				// can't
				throw new InvalidOperationException($"Cannot find reference to '{o}' in access expression {_scope.AccessExpression}");
			}

			// first, check variables
			foreach (var assignment in _scope.Assignments)
			{
				if (assignment.Name == o.ReferenceName)
				{
					return new Expression.Reference.Variable(o, assignment);
				}
			}

			// next, check method parameters
			foreach (var parameter in _method.Parameters)
			{
				if (parameter.Name == o.ReferenceName)
				{
					return new Expression.Reference.Parameter(o, parameter);
				}
			}

			// finally, check class fields
			if (_context != null)
			{
				foreach (var field in _context.Fields)
				{
					if (field.Name == o.ReferenceName)
					{
						return new Expression.Reference.Field(o, field);
					}
				}
			}

			throw new InvalidOperationException($"Cannot find reference to '{o}'");
		}

		private StringData Convert(Parser.StringData stringData)
		{
			var interpolations = new List<StringData.Interpolation>();

			foreach (var interpolation in stringData.Interpolations)
			{
				interpolations.Add(new StringData.Interpolation(Handle(interpolation.Expression), interpolation.Insert));
			}

			return new StringData(stringData.Value, interpolations);
		}
	}
}
