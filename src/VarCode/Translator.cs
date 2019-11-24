using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Terumi.Targets;

namespace Terumi.VarCode
{
	public class Diary
	{
		// we use a global dictionary for ALL field names so that we can have castability between two different types, eg.
		// if two types both have the field "ree", we can theoretically have a method that accepts in both and returns 'ree'
		public Dictionary<string, int> FieldNames { get; set; } = new Dictionary<string, int>();

		public List<Method> Methods { get; set; } = new List<Method>();
	}

	public class Translator
	{
		internal Diary _diary = new Diary();
		internal int _unique;
		internal readonly ICompilerTarget _target;

		public Translator(ICompilerTarget target)
		{
			_target = target;
		}

		public void TranslateLight(Binder.BoundFile file)
		{
			// first, match all fields to an id
			foreach (var @class in file.Classes)
			{
				foreach (var field in @class.Fields)
				{
					if (!_diary.FieldNames.ContainsKey(field.Name))
					{
						_diary.FieldNames[field.Name] = _unique++;
					}
				}
			}

			// next, take all class methods and put them as a Method (with a prefix ofc)
			foreach (var @class in file.Classes)
			{
				foreach (var method in @class.Methods)
				{
					var arguments = new List<Binder.MethodParameter>
					{
						new Binder.MethodParameter(@class, "self")
					};

					arguments.AddRange(method.Parameters);

					_diary.Methods.Add(new Method(@class, method as Binder.Method, $"<{@class.Name}>{method.Name}", arguments));
				}
			}

			foreach (var method in file.Methods)
			{
				_diary.Methods.Add(new Method(null, method, $"<>{method.Name}", method.Parameters));
			}
		}

		public void TranslateHard()
		{
			foreach (var method in _diary.Methods)
			{
				var morpher = new MethodMorpher(this, method);
				morpher.Bind();
				method.Code.AddRange(morpher._diary.Instructions);
			}
		}

		public class MethodMorpher
		{
			public class Diary
			{
				public List<Instruction> Instructions { get; set; } = new List<Instruction>();
				public Dictionary<string, int> Assignments { get; set; } = new Dictionary<string, int>();

				private int _unique;

				public int Unique() => _unique++;
			}

			private readonly Translator _translator;
			private readonly Method _method;
			internal Diary _diary = new Diary();

			private List<List<Instruction>> _instructionStack = new List<List<Instruction>>();

			public void IncreaseScope()
			{
				_instructionStack.Add(_diary.Instructions);
				_diary.Instructions = new List<Instruction>();
			}

			public List<Instruction> DecreaseScope()
			{
				var result = _diary.Instructions;
				_diary.Instructions = _instructionStack[^1];
				_instructionStack.RemoveAt(_instructionStack.Count - 1);
				return result;
			}

			public MethodMorpher(Translator translator, Method method)
			{
				_translator = translator;
				_method = method;
			}

			public void Bind() => Bind(_method.FromBinder.Body);

			public void Bind(Binder.CodeBody body)
			{
				foreach (var i in body.Statements)
				{
					Bind(i);
				}
			}

			public void Bind(Binder.Statement statement)
			{
				switch (statement)
				{
					case Binder.Statement.Access o:
					{
						Bind(o.Expression);
					}
					break;

					case Binder.Statement.Assignment o:
					{
						var valueId = Bind(o.Value);

						// first, let's figure out if we're assigning a field
						if (_method.Context != null)
						{
							if (_method.Context.Fields.Any(x => x.Name == o.Name && x.Type == o.Type))
							{
								// we're assigning a field
								var fieldId = _translator._diary.FieldNames[o.Name];

								var selfId = _diary.Unique();
								_diary.Instructions.Add(new Instruction.Load.Parameter(selfId, 0));

								_diary.Instructions.Add(new Instruction.SetField(selfId, fieldId, valueId));
								return;
							}
						}

						if (!_diary.Assignments.TryGetValue(o.Name, out var id))
						{
							id = _diary.Assignments[o.Name] = _diary.Unique();
						}

						_diary.Instructions.Add(new Instruction.Assign(id, valueId));
					}
					break;

					case Binder.Statement.Command o:
					{
						var stringData = ParseStringData(o.StringData);
						var command = _translator._target.Match(TargetMethodNames.Command, Binder.BuiltinType.String);

						_diary.Instructions.Add(new Instruction.CompilerCall(Instruction.Nowhere, command, new List<int> { stringData }));
					}
					break;

					case Binder.Statement.For o:
					{
						// a for loop operates as such:
						// 0: <init>
						// 1: <compare> (if false, jump 4)
						// 2: <code>
						// 3: <increment
						// 4: end

						IncreaseScope();
						Bind(o.Initialization);

						var comparison = Bind(o.Comparison);
						IncreaseScope();
						Bind(o.Code);
						Bind(o.End);
						_diary.Instructions.Add(new Instruction.Assign(comparison, Bind(o.Comparison)));

						var whileBody = DecreaseScope();
						_diary.Instructions.Add(new Instruction.While(comparison, whileBody));

						// we scoped it here just to scope the for loop initialization to the for loop,
						// but now we gotta unscope it
						var allBody = DecreaseScope();
						_diary.Instructions.AddRange(allBody);
					}
					break;

					case Binder.Statement.If o:
					{
						var comparison = Bind(o.Comparison);

						IncreaseScope();
						Bind(o.TrueClause);
						var trueClause = DecreaseScope();
						_diary.Instructions.Add(new Instruction.If(comparison, trueClause));

						if (o.ElseClause.Statements.Count > 0)
						{
							IncreaseScope();
							Bind(o.ElseClause);
							var elseClause = DecreaseScope();

							var not = _translator._target.Match(TargetMethodNames.OperatorNot, Binder.BuiltinType.Boolean);
							Debug.Assert(not != null);

							_diary.Instructions.Add(new Instruction.CompilerCall(comparison, not, new List<int> { comparison }));
							_diary.Instructions.Add(new Instruction.If(comparison, elseClause));
						}
					}
					break;

					case Binder.Statement.Increment o:
					{
						Bind(o.Expression);
					}
					break;

					case Binder.Statement.MethodCall o:
					{
						Bind(o.MethodCallExpression);
					}
					break;

					case Binder.Statement.Return o:
					{
						_diary.Instructions.Add(new Instruction.Return(o.Value == null ? Instruction.Nowhere : Bind(o.Value)));
					}
					break;

					case Binder.Statement.While o:
					{
						if (!o.IsDoWhile)
						{
							var comparison = Bind(o.Comparison);

							IncreaseScope();
							Bind(o.Body);

							// not only do we bind the body, we re-calculate the expression in the while
							_diary.Instructions.Add(new Instruction.Assign(comparison, Bind(o.Comparison)));
							var whileCode = DecreaseScope();

							_diary.Instructions.Add(new Instruction.While(comparison, whileCode));
						}
						else
						{
							// we set the comparison variable to 'true',
							// then run the while loop,
							// and then check the statement

							var comparison = _diary.Unique();
							_diary.Instructions.Add(new Instruction.Load.Boolean(comparison, true));

							IncreaseScope();
							Bind(o.Body);

							_diary.Instructions.Add(new Instruction.Assign(comparison, Bind(o.Comparison)));
							var whileCode = DecreaseScope();

							_diary.Instructions.Add(new Instruction.While(comparison, whileCode));
						}
					}
					break;
				}
			}

			public int Bind(Binder.Expression expression)
			{
				switch (expression)
				{
					case Binder.Expression.Access o:
					{
						var left = Bind(o.Left);
						Debug.Assert(left != Instruction.Nowhere);

						switch (o.Right)
						{
							case Binder.Expression.Reference.Field f:
							{
								return ReferenceField(f.FieldDeclaration.Name, left);
							}

							case Binder.Expression.MethodCall m:
							{
								var id = Instruction.Nowhere;

								if (m.Type != Binder.BuiltinType.Void)
								{
									id = _diary.Unique();
								}

								var method = _translator._diary.Methods.First(x => x.Name == $"<{o.Left.Type.TypeName}>{m.Calling.Name}"
									&& x.Parameters.Select(x => x.Type).SequenceEqual(m.Parameters.Select(x => x.Type).Prepend(o.Left.Type)));

								var args = new List<int> { left };

								foreach (var arg in m.Parameters)
								{
									var argId = Bind(arg);
									Debug.Assert(argId != Instruction.Nowhere);
									args.Add(argId);
								}

								_diary.Instructions.Add(new Instruction.Call(id, method, args));
								return id;
							}

							default: throw new InvalidOperationException();
						}
					}

					case Binder.Expression.Binary o:
					{
						var left = Bind(o.Left);
						Debug.Assert(left != Instruction.Nowhere);

						var right = Bind(o.Right);
						Debug.Assert(right != Instruction.Nowhere);

						var compilerMethod = _translator._target.Match(o.Operator.ToMethodName(), o.Left.Type, o.Right.Type);
						Debug.Assert(compilerMethod != null);

						var id = _diary.Unique();
						_diary.Instructions.Add(new Instruction.CompilerCall(id, compilerMethod, new List<int> { left, right }));
						return id;
					}

					case Binder.Expression.Constant o:
					{
						if (o.Value is Binder.StringData stringData) return ParseStringData(stringData);

						var id = _diary.Unique();

						switch (o.Value)
						{
							case Lexer.Number number:
							{
								_diary.Instructions.Add(new Instruction.Load.Number(id, number));
							}
							break;

							case bool b:
							{
								_diary.Instructions.Add(new Instruction.Load.Boolean(id, b));
							}
							break;

							default: throw new NotSupportedException();
						}

						return id;
					}

					case Binder.Expression.Increment o:
					{
						var bound = Bind(o.Expression);

						var one = _diary.Unique();
						_diary.Instructions.Add(new Instruction.Load.Number(one, new Lexer.Number(1)));

						switch (o.IncrementType)
						{
							case Binder.IncrementType.DecrementPost:
							{
								var target = _diary.Unique();
								_diary.Instructions.Add(new Instruction.Assign(target, bound));
								_diary.Instructions.Add(new Instruction.CompilerCall(bound, GetMethod(Binder.BinaryExpression.Subtract), new List<int> { bound, one }));
								return target;
							}

							case Binder.IncrementType.IncrementPost:
							{
								var target = _diary.Unique();
								_diary.Instructions.Add(new Instruction.Assign(target, bound));
								_diary.Instructions.Add(new Instruction.CompilerCall(bound, GetMethod(Binder.BinaryExpression.Add), new List<int> { bound, one }));
								return target;
							}

							// pre increments, we can just re-assign to the variable
							case Binder.IncrementType.DecrementPre:
							{
								_diary.Instructions.Add(new Instruction.CompilerCall(bound, GetMethod(Binder.BinaryExpression.Subtract), new List<int> { bound, one }));
								return bound;
							}

							case Binder.IncrementType.IncrementPre:
							{
								_diary.Instructions.Add(new Instruction.CompilerCall(bound, GetMethod(Binder.BinaryExpression.Add), new List<int> { bound, one }));
								return bound;
							}

							default: throw new NotImplementedException();
						}

						Binder.CompilerMethod GetMethod(Binder.BinaryExpression binaryExpression)
						{
							var compilerMethod = _translator._target.Match(binaryExpression.ToMethodName(), o.Expression.Type, Binder.BuiltinType.Number);
							Debug.Assert(compilerMethod != null);

							return compilerMethod;
						}
					}

					case Binder.Expression.MethodCall o:
					{
						var id = Instruction.Nowhere;

						if (o.Type != Binder.BuiltinType.Void)
						{
							id = _diary.Unique();
						}

						var args = new List<int>();

						if (_method.Context != null)
						{
							// `this`
							args.Add(Bind(new Binder.Expression.Reference.Parameter(null, _method.Parameters[0])));
						}

						foreach (var arg in o.Parameters)
						{
							var argId = Bind(arg);
							Debug.Assert(argId != Instruction.Nowhere);
							args.Add(argId);
						}

						if (o.Calling is Binder.CompilerMethod compilerMethod)
						{
							_diary.Instructions.Add(new Instruction.CompilerCall(id, compilerMethod, args));
							return id;
						}

						var method = _translator._diary.Methods.First(x => x.FromBinder == o.Calling);
						_diary.Instructions.Add(new Instruction.Call(id, method, args));

						return id;
					}

					case Binder.Expression.New o:
					{
						var id = _diary.Unique();

						_diary.Instructions.Add(new Instruction.New(id));
						var ctorMethod = _translator._diary.Methods.First(x => x.FromBinder == o.Constructor);

						var args = new List<int> { id };

						foreach (var arg in o.Parameters)
						{
							var argId = Bind(arg);
							Debug.Assert(argId != Instruction.Nowhere);
							args.Add(argId);
						}

						var result = _diary.Unique();
						_diary.Instructions.Add(new Instruction.Call(Instruction.Nowhere, ctorMethod, args));

						return id;
					}

					case Binder.Expression.Parenthesized o:
					{
						return Bind(o.Inner);
					}

					case Binder.Expression.Reference.Parameter o:
					{
						var id = _diary.Unique();

						var parameterIndex = _method.Parameters.IndexOf(_method.Parameters.First(x => x == o.MethodParameter));
						_diary.Instructions.Add(new Instruction.Load.Parameter(id, parameterIndex));

						return id;
					}

					case Binder.Expression.Reference.Variable o:
					{
						return _diary.Assignments[o.Declaration.Name];
					}

					case Binder.Expression.Reference.Field o:
					{
						return ReferenceField(o.FieldDeclaration.Name, null);
					}

					default: throw new NotSupportedException();
				}
			}

			private int	ParseStringData(Binder.StringData stringData)
			{
				var id = _diary.Unique();
				if (stringData.Interpolations.Count == 0)
				{
					_diary.Instructions.Add(new Instruction.Load.String(id, stringData.Value));
				}
				else
				{
					var add = _translator._target.Match(TargetMethodNames.OperatorAdd, Binder.BuiltinType.String, Binder.BuiltinType.String);
					Debug.Assert(add != null);

					// ...<>...<>...
					// we can break that into
					// ...
					// <>... (loop part)
					// <>...

					// let's first load in the first string
					ReadOnlySpan<char> data = stringData.Value;

					var str = data.Slice(0, stringData.Interpolations[0].Insert);
					_diary.Instructions.Add(new Instruction.Load.String(id, new string(str)));

					var load = _diary.Unique();

					// now iterate through every interpolation
					for (var i = 0; i < stringData.Interpolations.Count; i++)
					{
						// append expression
						var result = Bind(stringData.Interpolations[i].Expression);
						_diary.Instructions.Add(new Instruction.CompilerCall(id, add, new List<int> { id, result }));

						// calc the string to append
						var end = i == stringData.Interpolations.Count - 1 ? stringData.Value.Length : stringData.Interpolations[i + 1].Insert;
						str = data[(stringData.Interpolations[i].Insert)..(end)];

						// append string
						_diary.Instructions.Add(new Instruction.Load.String(load, new string(str)));
						_diary.Instructions.Add(new Instruction.CompilerCall(id, add, new List<int> { id, load }));
					}
				}
				return id;
			}

			private int ReferenceField(string fieldName, int? theSelf)
			{
				var id = _diary.Unique();

				int self;

				if (theSelf == null)
				{
					self = _diary.Unique();
					_diary.Instructions.Add(new Instruction.Load.Parameter(self, 0));
				}
				else
				{
					self = (int)theSelf;
				}

				var getField = new Instruction.GetField(id, self, _translator._diary.FieldNames[fieldName]);
				_diary.Instructions.Add(getField);

				return id;
			}
		}
	}
}
