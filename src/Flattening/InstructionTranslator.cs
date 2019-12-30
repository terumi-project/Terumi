using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.Binder;
using Terumi.Targets;

namespace Terumi.Flattening
{
	public class Scope
	{
		private readonly Maps _maps;
		private List<string> varLevels = new List<string>();
		private Dictionary<Statement.Declaration, string> _decls = new Dictionary<Statement.Declaration, string>();
		private int _unique;

		public Scope(Maps maps)
		{
			_maps = maps;
		}

		public void IncreaseScope(string scope) => varLevels.Add($"{scope}<{_unique++}>");
		public void DecreaseScope() => varLevels.RemoveAt(varLevels.Count - 1);

		public string GetThis() => Scope.SGetThis();

		public static string SGetThis()
		{
			return "$this$";
		}

		public string GetParameter(int index) => Scope.SGetParameter(index);

		public static string SGetParameter(int index)
		{
			return $"<><>param_{index}";
		}

		public void RegisterDeclaration(string varName, Statement.Declaration declaration)
		{
			_decls[declaration] = varName;
		}

		public string GetName()
		{
			var strb = new StringBuilder();

			strb.Append("<>");

			foreach (var level in varLevels)
			{
				strb.Append(level);
			}

			strb.Append("<>");

			strb.Append(_unique++);

			return strb.ToString();
		}

		public Method FindMethod(Binder.Method m)
		{
			foreach (var (boundMethod, (flattenedMethod, c)) in _maps.MethodMap)
			{
				if (m == boundMethod)
				{
					return flattenedMethod;
				}
			}

			throw new KeyNotFoundException();
		}

		public string GetVarName(Statement.Declaration declaration) => _decls[declaration];

		internal Method FindCodegennedCtor(IType target)
		{
			foreach (var item in _maps.Codegenned)
			{
				if (item?.Owner?.BoundClass == target)
				{
					return item;
				}
			}

			throw new KeyNotFoundException();
		}
	}

	public class InstructionTranslator
	{
		private List<Instruction> _target;
		private readonly Binder.Method _source;
		private readonly Scope _scope;

		public InstructionTranslator(Binder.Method source, List<Instruction> target, Scope scope)
		{
			_target = target;
			_source = source;
			_scope = scope;
		}

		public void Run()
		{
			Handle(_source.Body);
		}

		public void Handle(CodeBody statements)
		{
			foreach (var stmt in statements)
			{
				Handle(stmt);
			}
		}

		public void Handle(Statement stmt)
		{
			switch (stmt)
			{
				case Statement.Command o:
				{
					var strData = Handle(o.StringData);
					_target.Add(new Instruction.CompilerCall(null, new List<string> { strData }, TargetMethodNames.Command));
				}
				break;

				case Statement.Declaration o:
				{
					var name = Handle(o.Value);
					_scope.RegisterDeclaration(name, o);
				}
				break;

				case Statement.Return o:
				{
					if (o.Value == null)
					{
						_target.Add(new Instruction.Return(null));
					}
					else
					{
						_target.Add(new Instruction.Return(Handle(o.Value)));
					}
				}
				break;

				// simple dummy stmts
				case Statement.Assignment o:
				{
					Handle(o.AssignmentExpression);
				}
				break;

				case Statement.MethodCall o:
				{
					Handle(o.MethodCallExpression);
				}
				break;

				case Statement.Access o:
				{
					Handle(o.Expression);
				}
				break;

				case Statement.Increment o:
				{
					Handle(o.Expression);
				}
				break;

				// looping constructs
				case Statement.If o:
				{
					var comparison = Handle(o.Comparison);
					_scope.IncreaseScope("if");

					var tmpTarget = _target;
					var trueClause = new List<Instruction>();
					_target = trueClause;

					Handle(o.TrueClause);

					var elseClause = new List<Instruction>();
					_target = elseClause;

					Handle(o.ElseClause);

					_scope.DecreaseScope();
					_target = tmpTarget;
					_target.Add(new Instruction.If(comparison, trueClause, elseClause));
				}
				break;

				case Statement.For o:
				{
					_scope.IncreaseScope("for");

					Handle(o.Initialization);
					var comparison = Handle(o.Comparison);

					var whileBody = new List<Instruction>();
					var tmpTarget = _target;
					_target = whileBody;

					_scope.IncreaseScope("for-body");
					Handle(o.Code);
					Handle(o.End);
					var comparisonAgain = Handle(o.Comparison);
					_target.Add(new Instruction.Assignment(comparison, comparisonAgain));
					_scope.DecreaseScope();

					_target = tmpTarget;

					_target.Add(new Instruction.While(comparison, whileBody));
				}
				break;

				case Statement.While o:
				{
					string comparison;

					if (o.IsDoWhile)
					{
						comparison = _scope.GetName();
						_target.Add(new Instruction.LoadConstant(comparison, true));
					}
					else
					{
						comparison = Handle(o.Comparison);
					}

					var tmpTarget = _target;
					_target = new List<Instruction>();

					_scope.IncreaseScope("while");
					Handle(o.Body);

					var comparisonAgain = Handle(o.Comparison);
					_target.Add(new Instruction.Assignment(comparison, comparisonAgain));

					var @while = new Instruction.While(comparison, _target);
					_target = tmpTarget;

					_target.Add(@while);
				}
				break;
			}
		}

		public string Handle(Expression expression)
		{
			var result = _scope.GetName();

			switch (expression)
			{
				case Expression.Binary o:
				{
					_target.Add(new Instruction.CompilerCall(result, new List<string> { Handle(o.Left), Handle(o.Right) }, o.Operator.ToMethodName()));
				}
				break;

				case Expression.Unary o:
				{
					_target.Add(new Instruction.CompilerCall(result, new List<string> { Handle(o.Operand) }, o.Operator.ToMethodName()));
				}
				break;

				case Expression.Constant o:
				{
					switch (o.Value)
					{
						case bool b: _target.Add(new Instruction.LoadConstant(result, b)); break;
						case Number n: _target.Add(new Instruction.LoadConstant(result, n)); break;
						case Binder.StringData strDat: return Handle(strDat, result);
						default: throw new NotImplementedException();
					}
				}
				break;

				case Expression.Increment o:
				{
					// TODO: incrementing for fields
					if (!(o.Expression is Expression.Reference.Variable v))
					{
						throw new NotImplementedException();
					}

					var varName = _scope.GetVarName(v.Declaration);

					var one = _scope.GetName();
					_target.Add(new Instruction.LoadConstant(one, new Number(1)));
					_target.Add(new Instruction.Assignment(result, varName));

					switch (o.IncrementType)
					{
						// x--
						// returns: 'x'
						// mutation: 'x -= 1'
						case IncrementType.DecrementPost:
						{
							_target.Add(new Instruction.CompilerCall(varName, new List<string> { varName, one }, TargetMethodNames.OperatorSubtract));
							return result;
						}

						case IncrementType.IncrementPost:
						{
							_target.Add(new Instruction.CompilerCall(varName, new List<string> { varName, one }, TargetMethodNames.OperatorAdd));
							return result;
						}
					}

					throw new NotImplementedException();
				}
				break;

				case Expression.New o:
				{
					_target.Add(new Instruction.New(result));

					if (o.Constructor == null)
					{
						// use codegenned ctor
						var ctor = _scope.FindCodegennedCtor(o.Target);
						_target.Add(new Instruction.MethodCall(null, EmptyList<string>.Instance, ctor, result));
					}
					else
					{
						var args = new List<string>();

						foreach (var arg in o.Parameters)
						{
							args.Add(Handle(arg));
						}

						_target.Add(new Instruction.MethodCall(null, args, _scope.FindMethod(o.Constructor as Binder.Method), result));
					}
				}
				break;

				case Expression.Parenthesized p:
				{
					return Handle(p.Inner);
				}

				case Expression.Access o:
				{
					var left = Handle(o.Left);

					switch (o.Right)
					{
						case Expression.Reference.Field p:
						{
							// access a field of 'left'
							_target.Add(new Instruction.Dereference(result, left, p.FieldDeclaration));
						}
						break;

						case Expression.MethodCall p:
						{
							var args = new List<string>();

							foreach (var arg in p.Parameters)
							{
								args.Add(Handle(arg));
							}

							// call a method on 'left'
							switch (p.Calling)
							{
								case CompilerMethod m:
								{
									// weird, but we'll deal with it by adding the instance as the first arg
									args.Insert(0, left);
									_target.Add(new Instruction.CompilerCall(m.ReturnType == BuiltinType.Void ? null : result, args, m.Name));
								}
								break;

								case Binder.Method m:
								{
									var method = _scope.FindMethod(m);

									_target.Add(new Instruction.MethodCall(method.BoundMethod.ReturnType == BuiltinType.Void ? null : result, args, method, left));
								}
								break;

								default: throw new NotImplementedException();
							}
						}
						break;

						default: throw new NotImplementedException();
					}
				}
				break;

				case Expression.Assignment o:
				{
					var value = Handle(o.Right);

					if (o.Left is Expression.Access p)
					{
						if (p.Right is Expression.Reference.Field f)
						{
							// we need to assign a FIELD to whatever value
							var obj = Handle(p.Left);
							_target.Add(new Instruction.SetField(obj, f.FieldDeclaration, value));
							break;
						}

						throw new NotImplementedException();
					}

					// we're setting a variable
					if (o.Left is Expression.Reference.Variable varRef)
					{
						var varName = _scope.GetVarName(varRef.Declaration);
						_target.Add(new Instruction.Assignment(varName, value));
						break;
					}

					if (o.Left is Expression.Reference.Field field)
					{
						_target.Add(new Instruction.SetField(_scope.GetThis(), field.FieldDeclaration, value));
						break;
					}

					throw new NotImplementedException();
				}

				case Expression.Reference o:
				{
					switch (o)
					{
						case Expression.Reference.Variable r:
						{
							return _scope.GetVarName(r.Declaration);
						}

						case Expression.Reference.Parameter r:
						{
							return _scope.GetParameter(_source.Parameters.FindIndex(p => p == r.MethodParameter));
						}

						case Expression.Reference.Field f:
						{
							_target.Add(new Instruction.Dereference(result, _scope.GetThis(), f.FieldDeclaration));
							return result;
						}

						default: throw new NotImplementedException();
					}

					throw new NotImplementedException();
				}

				case Expression.MethodCall o:
				{
					var args = new List<string>();

					foreach (var arg in o.Parameters)
					{
						args.Add(Handle(arg));
					}

					if (o.Calling.IsCompilerDefined)
					{
						_target.Add(new Instruction.CompilerCall(o.Calling.ReturnType == BuiltinType.Void ? null : result, args, o.Calling.Name));
					}
					else
					{
						string scop = null;
						var targetMethod = _scope.FindMethod(o.Calling as Binder.Method);

						if (targetMethod.Owner != null)
						{
							scop = _scope.GetThis();
						}

						_target.Add(new Instruction.MethodCall(targetMethod.BoundMethod.ReturnType == BuiltinType.Void ? null : result, args, targetMethod, scop));
					}
				}
				break;

				default: throw new NotImplementedException();
			}

			return result;
		}

		// returns a string representing the variable name of which the interpolated stringdata belongs to
		public string Handle(StringData strData, string? result = null)
		{
			result ??= _scope.GetName();

			if (strData.Interpolations.Count == 0)
			{
				// great! just load a string
				result = _scope.GetName();

				_target.Add(new Instruction.LoadConstant(result, strData.Value.ToString()));

				return result;
			}
			else
			{
				ReadOnlySpan<char> str = strData.Value.ToString();
				var chrs = str.Slice(0, strData.Interpolations[0].Insert);

				var total = _scope.GetName();
				_target.Add(new Instruction.LoadConstant(total, ""));

				// load the first part of the string
				var loadStr = _scope.GetName();
				_target.Add(new Instruction.LoadConstant(loadStr, chrs.ToString()));

				Concat(total, loadStr);

				for (var i = 0; i < strData.Interpolations.Count; i++)
				{
					// handle the interpolation,
					// then concat it with whatever we've got
					var interpolation = Handle(strData.Interpolations[i].Expression);
					Concat(total, interpolation);

					int next;
					if (i + 1 < strData.Interpolations.Count)
					{
						next = strData.Interpolations[i + 1].Insert;
					}
					else
					{
						next = str.Length;
					}

					var s = strData.Interpolations[i].Insert;
					var tilNext = str.Slice(s, next - s);

					_target.Add(new Instruction.LoadConstant(loadStr, tilNext.ToString()));
					Concat(total, loadStr);
				}

				return total;

				void Concat(string a, string b)
				{
					_target.Add(new Instruction.CompilerCall(a, new List<string> { a, b }, TargetMethodNames.OperatorAdd));
				}
			}
		}
	}
}
