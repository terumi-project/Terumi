using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
				public Dictionary<Binder.Statement.Assignment, int> Assignments { get; set; } = new Dictionary<Binder.Statement.Assignment, int>();

				private int _unique;

				public int Unique() => _unique++;
			}

			private readonly Translator _translator;
			private readonly Method _method;
			internal Diary _diary = new Diary();

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

						if (!_diary.Assignments.TryGetValue(o, out var id))
						{
							id = _diary.Assignments[o] = _diary.Unique();
						}

						_diary.Instructions.Add(new Instruction.Assign(id, valueId));
					}
					break;

					case Binder.Statement.Command o:
					{
						throw new NotImplementedException();
					}
					break;

					case Binder.Statement.For o:
					{
						throw new NotImplementedException();
					}
					break;

					case Binder.Statement.If o:
					{
						throw new NotImplementedException();
					}
					break;

					case Binder.Statement.Increment o:
					{
						throw new NotImplementedException();
					}
					break;

					case Binder.Statement.MethodCall o:
					{
						Bind(o.MethodCallExpression);
					}
					break;

					case Binder.Statement.Return o:
					{
						_diary.Instructions.Add(new Instruction.Return(Bind(o.Value)));
					}
					break;

					case Binder.Statement.While o:
					{
						throw new NotImplementedException();
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
								var id = _diary.Unique();

								var method = _translator._diary.Methods.First(x => x.Name == $"<{o.Left.Type.TypeName}>{m.Calling.Name}");

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
						var left = Bind(o);
						Debug.Assert(left != Instruction.Nowhere);

						var right = Bind(o);
						Debug.Assert(right != Instruction.Nowhere);

						throw new NotImplementedException();
						return Instruction.Nowhere;
					}

					case Binder.Expression.Constant o:
					{
						switch (o.Value)
						{
							case Binder.StringData data:
							{
								var id = _diary.Unique();
								_diary.Instructions.Add(new Instruction.Load.String(id, data.Value));
								return id;
							}
						}

						throw new NotSupportedException();
					}

					case Binder.Expression.Increment o:
					{
						throw new NotImplementedException();
						return Instruction.Nowhere;
					}

					case Binder.Expression.MethodCall o:
					{
						var id = Instruction.Nowhere;

						if (o.Type != Binder.BuiltinType.Void)
						{
							id = _diary.Unique();
						}

						var args = new List<int>();

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
						_diary.Instructions.Add(new Instruction.Call(result, ctorMethod, args));

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
						return _diary.Assignments[o.Declaration];
					}

					case Binder.Expression.Reference.Field o:
					{
						return ReferenceField(o.FieldDeclaration.Name, null);
					}

					default: throw new NotSupportedException();
				}
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
