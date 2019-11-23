using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.Binder;
using Terumi.Targets;

namespace Terumi.VarCode
{
	public class Translator
	{
		public class Diary
		{
			public Dictionary<Field, int> FieldMappings { get; set; } = new Dictionary<Field, int>();
			public Dictionary<(string, IMethod), int> MethodMappings { get; set; } = new Dictionary<(string, IMethod), int>();
			public List<InstructionMethod> Methods { get; set; } = new List<InstructionMethod>();

			public int GetFieldId(Field f, ref int unique)
			{
				if (FieldMappings.TryGetValue(f, out var result)) return result;
				FieldMappings[f] = result = unique++;
				return result;
			}

			public int GetMethodId(string prepend, IMethod method, ref int unique)
			{
				if (MethodMappings.TryGetValue((prepend, method), out var result)) return result;
				MethodMappings[(prepend, method)] = result = unique++;
				return result;
			}
		}

		public Translator(ICompilerTarget target)
		{
			_target = target;
		}

		internal Diary _diary = new Diary();
		internal int _unique;
		internal readonly ICompilerTarget _target;

		public void Visit(BoundFile file)
		{
			foreach (var @class in file.Classes)
			{
				foreach (var method in @class.Methods)
				{
					// TODO: encapsulate this way of getting method ids (<>global_ & classname_)
					var id = _diary.GetMethodId(@class.Name + "_", method, ref _unique);

					System.Diagnostics.Debug.Assert(method is Method, "Class methods must be Method");

					var gear = new Gears.Method(this, id, @class, method as Method, _unique++);
					gear.Bind();
				}
			}

			foreach (var method in file.Methods)
			{
				var id = _diary.GetMethodId("<>global_", method, ref _unique);
				var gear = new Gears.Method(this, id, null, method, -1);
				gear.Bind();
			}
		}

		public abstract class Gears
		{
			public class Method : Gears
			{
				private readonly Translator _parent;
				private readonly Class? _context;
				private readonly Binder.Method _method;
				private readonly List<Instruction> _instructions = new List<Instruction>();
				private readonly int _selfMethodId;
				private readonly int _thisReference;
				private readonly List<int> _methodParams;
				private readonly Dictionary<Statement.Assignment, int> _vars = new Dictionary<Statement.Assignment, int>();

				public Method(Translator parent, int id, Class? context, Binder.Method method, int thisReference)
				{
					_parent = parent;
					_context = context;
					_method = method;
					_selfMethodId = id;
					_thisReference = thisReference;
					_methodParams = _method.Parameters.Select(x => _parent._unique++).ToList();
				}

				public void Bind()
				{
					foreach (var line in _method.Body.Statements)
					{
						BindStatement(line);
					}

					_parent._diary.Methods[_selfMethodId] = new InstructionMethod
					(
						_thisReference == -1 ? _methodParams : new int[] { _thisReference }.Concat(_methodParams).ToList(),
						new InstructionBody(_instructions)
					);
				}

				public void BindStatement(Statement stmt)
				{
					switch (stmt)
					{
						case Statement.Access o: break;
						case Statement.Assignment o: break;
						case Statement.Command o: break;
						case Statement.For o: break;
						case Statement.If o: break;
						case Statement.Increment o: break;
						case Statement.MethodCall o: break;
						case Statement.Return o: break;
						case Statement.While o: break;
					}
				}

				public int BindExpression(Expression e)
				{
					switch (e)
					{
						case Expression.Access o:
						{
							// TODO: accesses need to have a ref to their self
							var left = BindExpression(o.Left);

							if (o.Right is Expression.MethodCall m)
							{
								var methodId = _parent._diary.GetMethodId(o.Left.Type.TypeName + "_", m.Calling, ref _parent._unique);

								return BindMethod(methodId, m);
							}
							else if (o.Right is Expression.Reference.Field f)
							{
								var fieldId = _parent._diary.GetFieldId(f.FieldDeclaration, ref _parent._unique);
								var getField = new Instruction.GetField(left, fieldId, _parent._unique++);
								_instructions.Add(getField);

								return getField.StoreValue;
							}
							else
							{
								throw new InvalidOperationException();
							}
						}

						case Expression.Binary o:
						{
							var left = BindExpression(o.Left);
							var right = BindExpression(o.Right);
							var methodOp = this.GetBinaryOperand(o.Operator, new List<Expression> { o.Left, o.Right });
							System.Diagnostics.Debug.Assert(methodOp != null, "comp comp method cant b null");

							var methodId = _parent._diary.GetMethodId("<>comp_", methodOp, ref _parent._unique);

							var resultId = _parent._unique++;
							var call = new Instruction.MethodCall(resultId, methodId, new List<int> { left, right });
							_instructions.Add(call);
							return resultId;
						}

						case Expression.Constant o:
						{
							var resultId = _parent._unique++;

							// TODO: for StringData values, convert them into multiple assignments and concatenations
							_instructions.Add(new Instruction.Assignment.Constant(o.Value));

							return resultId;
						}

						case Expression.Increment o:
						{
							// need binary instructions
							throw new InvalidOperationException();
						}

						case Expression.MethodCall o:
						return BindMethod(_parent._diary.GetMethodId(o.Calling.IsCompilerDefined ? "<>comp_" : "<>global_", o.Calling, ref _parent._unique), o);

						case Expression.New o:
						{
							// TODO: ctors need to have a ref to their self
							var id = _parent._unique++;
							_instructions.Add(new Instruction.Assignment.New(id));

							var args = new List<int>();

							foreach (var arg in o.Parameters)
							{
								args.Add(BindExpression(arg));
							}

							var ctorId = _parent._diary.GetMethodId(o.Type.TypeName + "_", o.Constructor, ref _parent._unique);
							var resultId = GetMethodCallResultId(o.Type);

							return BindMethod(ctorId, args, resultId);
						}

						case Expression.Parenthesized o: return BindExpression(o.Inner);

						case Expression.Reference.Parameter o:
						{
							var id = _method.Parameters.IndexOf(o.MethodParameter);
							return _methodParams[id];
						}

						case Expression.Reference.Variable o: return GetVariable(o.Declaration);

						case Expression.Reference.Field o:
						{
							if (_thisReference == -1) throw new InvalidOperationException("cant ref field if no this ref");

							var fieldId = _parent._diary.GetFieldId(o.FieldDeclaration, ref _parent._unique);
							var getField = new Instruction.GetField(_thisReference, fieldId, _parent._unique++);

							return getField.StoreValue;
						}

						default: throw new InvalidOperationException();
					}
				}

				public int BindMethod(int methodId, Expression.MethodCall m)
				{
					var args = new List<int>();

					foreach (var arg in m.Parameters)
					{
						args.Add(BindExpression(arg));
					}

					var resultId = GetMethodCallResultId(m);

					return BindMethod(methodId, args, resultId);
				}

				public int BindMethod(int methodId, List<int> args, int resultId)
				{
					_instructions.Add(new Instruction.MethodCall(resultId, methodId, args));

					return resultId;
				}

				private int GetMethodCallResultId(Expression.MethodCall m)
					=> GetMethodCallResultId(m.Type);

				private int GetMethodCallResultId(IType m)
				{
					if (m == BuiltinType.Void)
					{
						return -1;
					}

					return _parent._unique++;
				}

				private int GetVariable(Statement.Assignment assignment)
				{
					if (_vars.TryGetValue(assignment, out var id)) return id;
					_vars[assignment] = id = _parent._unique++;
					return id;
				}

				private IMethod GetBinaryOperand(BinaryExpression @operator, List<Expression> arguments)
				{
					switch (@operator)
					{
						case BinaryExpression.Not: throw new NotImplementedException();
						case BinaryExpression.EqualTo: return _parent._target.Match(TargetMethodNames.OperatorEqualTo, arguments);
						case BinaryExpression.NotEqualTo: return _parent._target.Match(TargetMethodNames.OperatorNotEqualTo, arguments);
						case BinaryExpression.LessThan: return _parent._target.Match(TargetMethodNames.OperatorLessThan, arguments);
						case BinaryExpression.LessThanOrEqualTo: return _parent._target.Match(TargetMethodNames.OperatorLessThanOrEqualTo, arguments);
						case BinaryExpression.GreaterThan: return _parent._target.Match(TargetMethodNames.OperatorGreaterThan, arguments);
						case BinaryExpression.GreaterThanOrEqualTo: return _parent._target.Match(TargetMethodNames.OperatorGreaterThanOrEqualTo, arguments);
						case BinaryExpression.Add: return _parent._target.Match(TargetMethodNames.OperatorAdd, arguments);
						case BinaryExpression.Subtract: return _parent._target.Match(TargetMethodNames.OperatorSubtract, arguments);
						case BinaryExpression.Multiply: return _parent._target.Match(TargetMethodNames.OperatorMultiply, arguments);
						case BinaryExpression.Divide: return _parent._target.Match(TargetMethodNames.OperatorDivide, arguments);
						case BinaryExpression.Exponent: return _parent._target.Match(TargetMethodNames.OperatorExponent, arguments);
					}

					throw new NotImplementedException();
				}
			}
		}
	}
}
