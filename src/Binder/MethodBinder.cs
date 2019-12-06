using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Terumi.Parser;

namespace Terumi.Binder
{
	public class Scope
	{
		private readonly Dictionary<string, Statement.Declaration> _vars = new Dictionary<string, Statement.Declaration>();
		private readonly Dictionary<string, MethodParameter> _parameters = new Dictionary<string, MethodParameter>();
		private readonly Dictionary<string, Field> _fields = new Dictionary<string, Field>();
		private readonly Scope? _previous;

		public Scope(Class? classCtx, Method? methodCtx, Scope? previous = null)
		{
			_previous = previous;

			if (classCtx != null)
			{
				foreach (var thing in classCtx.Fields)
				{
					_fields[thing.Name] = thing;
				}
			}

			if (methodCtx != null)
			{
				foreach (var thing in methodCtx.Parameters)
				{
					_parameters[thing.Name] = thing;
				}
			}
		}

		public bool TryGetReference(Parser.Expression.Reference parserReference, string name, out Expression.Reference reference)
		{
			if (_previous != null
				&& _previous.TryGetReference(parserReference, name, out reference))
			{
				return true;
			}

			if (_fields.TryGetValue(name, out var field))
			{
				reference = new Expression.Reference.Field(parserReference, field);
				return true;
			}

			if (_parameters.TryGetValue(name, out var parameter))
			{
				reference = new Expression.Reference.Parameter(parserReference, parameter);
				return true;
			}

			if (_vars.TryGetValue(name, out var decl))
			{
				reference = new Expression.Reference.Variable(parserReference, decl);
				return true;
			}

			reference = default;
			return false;
		}

		public bool TryGetVariable(string name, out Statement.Declaration declaration)
		{
			if (_previous != null
				&& _previous.TryGetVariable(name, out declaration))
			{
				return true;
			}

			return _vars.TryGetValue(name, out declaration);
		}

		public bool TryDeclare(string name, Statement.Declaration declaration)
		{
			if (TryGetVariable(name, out _))
			{
				return false;
			}

			_vars[name] = declaration;
			return true;
		}
	}

	public class MethodBinder
	{
		internal readonly TerumiBinder _parent;
		internal readonly Class _context;
		internal readonly Method _method;
		internal readonly SourceFile _file;
		internal readonly Scope _scope;

		public MethodBinder(TerumiBinder parent, Class? context, Method method, SourceFile file)
		{
			_parent = parent;
			_context = context;
			_method = method;
			_file = file;
			_scope = new Scope(_context, _method);
		}

		public CodeBody Finalize() => new CodeBody(Handle(_scope, _method.FromParser.Code));

		private List<Statement> Handle(Scope scope, Parser.CodeBody body)
		{
			var binder = new CodeBodyBinder(this, scope);
			return binder.Handle(body);
		}
	}

	public class CodeBodyBinder
	{
		private readonly MethodBinder _parent;
		private Scope _scope;

		public CodeBodyBinder(MethodBinder parent, Scope scope)
		{
			_parent = parent;
			_scope = scope;
		}

		public List<Statement> Handle(Parser.CodeBody body)
		{
			var stmts = new List<Statement>();

			foreach (var stmt in body.Statements)
			{
				stmts.AddRange(Handle(stmt));
			}

			return stmts;
		}

		// TODO: prevent lots of list allocations, but as of now, meh
		private List<Statement> Handle(Parser.Statement stmt)
		{
			switch (stmt)
			{
				case Parser.Statement.Access o: return HandleOne(Handle(o));
				case Parser.Statement.Assignment o: return HandleOne(Handle(o));
				case Parser.Statement.Command o: return HandleOne(Handle(o));
				case Parser.Statement.Declaration o: return HandleOne(Handle(o));
				case Parser.Statement.For o: return HandleOne(Handle(o));
				case Parser.Statement.If o: return HandleOne(Handle(o));
				case Parser.Statement.Increment o: return HandleOne(Handle(o));
				case Parser.Statement.MethodCall o: return HandleOne(Handle(o));
				case Parser.Statement.Return o: return HandleOne(Handle(o));
				case Parser.Statement.While o: return HandleOne(Handle(o));
				default: throw new NotSupportedException(stmt.GetType().FullName);
			}

			static List<Statement> HandleOne(Statement statement) => new List<Statement> { statement };
		}

		private Statement.Access Handle(Parser.Statement.Access o)
		{
			return new Statement.Access(o, Handle(o.AccessExpression));
		}

		private Statement.Assignment Handle(Parser.Statement.Assignment o)
		{
			return new Statement.Assignment(o, Handle(o.Expression));
		}

		private Statement.Command Handle(Parser.Statement.Command o)
		{
			return new Statement.Command(o, Update(o.String));
		}

		private Statement.Declaration Handle(Parser.Statement.Declaration o)
		{
			var value = Handle(o.Value);

			if (o.Type == null)
			{
				return Declare(value.Type);
			}

			var supposeToBe = _parent._parent.FindImmediateType(o.Type, _parent._file);

			if (!_parent._parent.CanUseTypeAsType(supposeToBe, value.Type))
			{
				throw new CodeBinderException(o, $"Unable to use type '{supposeToBe}' as '{value.Type}'");
			}

			return Declare(supposeToBe);

			Statement.Declaration Declare(IType type)
			{
				var decl = new Statement.Declaration(o, type, o.Name, value);

				if (!_scope.TryDeclare(o.Name, decl))
				{
					throw new CodeBinderException(o, "Unable to declare variable");
				}

				return decl;
			}
		}

		private Statement.For Handle(Parser.Statement.For o)
		{
			var prevScope = _scope;
			_scope = new Scope(null, null, prevScope);

			var init = Handle(o.Declaration);
			var cmp = Handle(o.Comparison);
			var body = Handle(o.Statements);
			var inc = Handle(o.End);

			_scope = prevScope;

			return new Statement.For(o, new CodeBody(init), cmp, new CodeBody(inc), new CodeBody(body));
		}

		private Statement.If Handle(Parser.Statement.If o)
		{
			var cmp = Handle(o.Comparison);

			List<Statement> primary;
			List<Statement> secondary;

			{
				var prevScope = _scope;
				_scope = new Scope(null, null, prevScope);

				primary = Handle(o.IfClause);

				_scope = prevScope;
			}

			{
				var prevScope = _scope;
				_scope = new Scope(null, null, prevScope);

				secondary = Handle(o.ElseClause);

				_scope = prevScope;
			}

			return new Statement.If(o, cmp, new CodeBody(primary), new CodeBody(secondary));
		}

		private Statement.Increment Handle(Parser.Statement.Increment o)
		{
			return new Statement.Increment(o, Handle(o.IncrementExpression));
		}

		private Statement.MethodCall Handle(Parser.Statement.MethodCall o)
		{
			return new Statement.MethodCall(o, Handle(o.MethodCallExpression));
		}

		private Statement.Return Handle(Parser.Statement.Return o)
		{
			var value = o.Expression == null ? null : Handle(o.Expression);
			return new Statement.Return(o, value);
		}

		private Statement.While Handle(Parser.Statement.While o)
		{
			var prevScope = _scope;
			_scope = new Scope(null, null, prevScope);

			List<Statement> body;
			Expression cmp;

			if (o.IsDoWhile)
			{
				body = Handle(o.Statements);
				cmp = Handle(o.Comparison);
			}
			else
			{
				cmp = Handle(o.Comparison);
				body = Handle(o.Statements);
			}

			_scope = prevScope;
			return new Statement.While(o, o.IsDoWhile, cmp, new CodeBody(body));
		}

		// expressions
		private Expression Handle(Parser.Expression expression)
		{
			switch (expression)
			{
				case Parser.Expression.Access o: return Handle(o);
				case Parser.Expression.Assignment o: return Handle(o);
				case Parser.Expression.Binary o: return Handle(o);
				case Parser.Expression.Constant o: return Handle(o);
				case Parser.Expression.Increment o: return Handle(o);
				case Parser.Expression.MethodCall o: return Handle(o);
				case Parser.Expression.New o: return Handle(o);
				case Parser.Expression.Parenthesized o: return Handle(o);
				case Parser.Expression.Reference o: return Handle(o);
				case Parser.Expression.Unary o: return Handle(o);
				default: throw new NotSupportedException(expression.GetType().FullName);
			}
		}

		private Expression.Access Handle(Parser.Expression.Access o)
		{
			var left = Handle(o.Left);

			switch (o.Right)
			{
				// field reference
				case Parser.Expression.Reference p:
				{
					var fieldName = p.ReferenceName;
					var targetField = left.Type.Fields.First(x => x.Name == fieldName);

					return new Expression.Access(o, left, new Expression.Reference.Field(p, targetField));
				}

				// method call on the object
				case Parser.Expression.MethodCall p:
				{
					if (p.IsCompilerCall)
					{
						throw new CodeBinderException(p, "Cannot call compiler method calls on objects");
					}

					var exprs = new List<Expression>();

					foreach (var expr in p.Arguments)
					{
						exprs.Add(Handle(expr));
					}

					// find the right method
					var method = _parent._parent.TryFindMethod(left.Type.Methods, p.Name, exprs);

					if (method == null)
					{
						throw new CodeBinderException(p, "Unable to find method call");
					}

					return new Expression.Access(o, left, new Expression.MethodCall(p, method, exprs));
				}
			}

			throw new CodeBinderException(o, "Unsupported expression on access");
		}

		private Expression.Assignment Handle(Parser.Expression.Assignment o)
		{
			var left = Handle(o.Left);
			var right = Handle(o.Right);

			switch (left)
			{
				case Expression.Reference p:
				{
				}
				break;

				case Expression.Access p:
				{
				}
				break;

				default: throw new CodeBinderException(o, "Cannot handle assignnment");
			}

			return new Expression.Assignment(o, left, right);
		}

		private Expression.Binary Handle(Parser.Expression.Binary o)
		{
			var left = Handle(o.Left);
			var right = Handle(o.Right);

			return new Expression.Binary(o, left, o.Operator.ToBinaryExpression(), right);
		}

		private Expression.Constant Handle(Parser.Expression.Constant o)
		{
#if DEBUG
			Debug.Assert(o.Value is Parser.StringData
				|| o.Value is Number
				|| o.Value is bool);
#endif
			if (o.Value is Parser.StringData strDat)
			{
				var up = Update(strDat);
				return new Expression.Constant(o, up);
			}

			return new Expression.Constant(o, o.Value);
		}

		private Expression.Increment Handle(Parser.Expression.Increment o)
		{
			return new Expression.Increment(o, Handle(o.Expression), o.Side.ToIncrementType(o.Type));
		}

		private Expression.MethodCall Handle(Parser.Expression.MethodCall o)
		{
			var exprs = new List<Expression>();

			foreach (var expr in o.Arguments)
			{
				exprs.Add(Handle(expr));
			}

			if (!_parent._parent.FindImmediateMethod(_parent._context, o, exprs, out var method))
			{
				throw new CodeBinderException(o, $"Couldn't find exception for method call {o.Name}");
			}

			return new Expression.MethodCall(o, method, exprs);
		}

		private Expression.New Handle(Parser.Expression.New o)
		{
			var type = _parent._parent.FindImmediateType(o.Type, _parent._file);

			var exprs = new List<Expression>();

			foreach (var expr in o.Expressions)
			{
				exprs.Add(Handle(expr));
			}

			var ctor = _parent._parent.TryFindConsructor(type, exprs);

			return new Expression.New(o, type, ctor, exprs);
		}

		private Expression.Parenthesized Handle(Parser.Expression.Parenthesized o)
		{
			return new Expression.Parenthesized(o, Handle(o.Inner));
		}

		private Expression.Reference Handle(Parser.Expression.Reference o)
		{
			if (!_scope.TryGetReference(o, o.ReferenceName, out var varRef))
			{
				throw new CodeBinderException(o, $"Unable to find reference '{o.ReferenceName}'");
			}

			return varRef;
		}

		private Expression.Unary Handle(Parser.Expression.Unary o)
		{
			var unary = o.Operator.ToUnaryExpression();
			var value = Handle(o.Operand);

			return new Expression.Unary(o, unary, value);
		}

		/* helpers */
		private StringData Update(Parser.StringData stringData)
		{
			var intpls = new List<StringData.Interpolation>();

			foreach (var interpolation in stringData.Interpolations)
			{
				var newExpr = Handle(interpolation.Expression);

				intpls.Add(new StringData.Interpolation(newExpr, interpolation.Insert));
			}

			return new StringData(stringData.Value, intpls);
		}
	}

	public class CodeBinderException : Exception
	{
		public CodeBinderException(Parser.Statement stmt, string message) : base(message)
		{
			Stmt = stmt;
		}

		public CodeBinderException(Parser.Expression expr, string message) : base(message)
		{
			Expr = expr;
		}

		public Parser.Statement Stmt { get; }
		public Parser.Expression Expr { get; }
	}

	/*
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

			var registerVar = true;
			if (_context != null)
			{
				// we may be setting a field
				if (_context.Fields.Any(x => x.Name == o.Name))
				{
					var field = _context.Fields.First(x => x.Name == o.Name);
					registerVar = false;
				}
			}

			System.Diagnostics.Debug.Assert(type != BuiltinType.Void, "Assignment cannot result in a void type");
			var assignment = new Statement.Assignment(o, type, o.Name, value);

			if (registerVar) _scope.RegisterVariable(assignment);
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
			=> new Statement.Return(o, o.Expression == null ? null : Handle(o.Expression));

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

		public Expression Handle(Parser.Expression expr, bool clearAccess = true)
		{
			var acc = _scope.AccessExpression;

			if (clearAccess) _scope.AccessExpression = null;

			Expression result;

			switch (expr)
			{
				case Parser.Expression.Access o: result = Handle(o); break;
				case Parser.Expression.Binary o: result = Handle(o); break;
				case Parser.Expression.Constant o: result = Handle(o); break;
				case Parser.Expression.Increment o: result = Handle(o); break;
				case Parser.Expression.MethodCall o: result = Handle(o); break;
				case Parser.Expression.New o: result = Handle(o); break;
				case Parser.Expression.Parenthesized o: result = Handle(o); break;
				case Parser.Expression.Reference o: result = Handle(o); break;
				default: throw new NotSupportedException($"{expr.GetType()}");
			}

			_scope.AccessExpression = acc;

			return result;
		}

		public Expression.Access Handle(Parser.Expression.Access o)
		{
			// increase scope specifically for the access expression stuff
			IncreaseScope();
			var access = Handle(o.Left);
			_scope.AccessExpression = access;
			var right = Handle(o.Right, false);
			_scope.AccessExpression = null;
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
				if (_parent.FindMethod(o.IsCompilerCall, o.Name, exprs, _scope.AccessExpression.Type.Methods, out var accessMethod))
				{
					return new Expression.MethodCall(o, accessMethod, exprs);
				}
			}

			if (_context != null)
			{
				if (_parent.FindMethod(o.IsCompilerCall, o.Name, exprs, _context.Methods, out var targetMethod))
				{
					return new Expression.MethodCall(o, targetMethod, exprs);
				}
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
	*/
}
