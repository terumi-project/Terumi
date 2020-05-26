using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.Binder;
using Terumi.Flattening;
using Terumi.Targets;

namespace Terumi.Deobjectification
{
	public class Deobjectifier
	{
		private readonly Binder.TerumiBinderBindings _bindings;
		private readonly FlattenedProject _flattened;
		private readonly ICompilerTarget _target;

		public Deobjectifier(Binder.TerumiBinderBindings bindings, FlattenedProject flattened, ICompilerTarget target)
		{
			_bindings = bindings;
			_flattened = flattened;
			_target = target;
		}

		public List<VarCode.Method> Translate(out int objectFields)
		{
			var newMethods = new List<VarCode.Method>();

			Log.Stage("DEOBJ", "Deobjectifying flattened code");

			// map each class to have a number identifying their type
			// the index is their type
			var classMap = new List<Flattening.Class>();

			// first, we're going to build a giant single global object
			var map = new List<(ObjectType, string)>();

			map.Add((ObjectType.Number, "<>_breaker_type"));

			foreach (var @class in _flattened.Classes)
			{
				classMap.Add(@class);

				foreach (var field in @class.Fields)
				{
					var item = (field.Type, field.ToWeirdName());

					if (!map.Contains(item))
					{
						map.Add(item);
					}
				}
			}

			// let's setup the field map to be by id
			var fieldMap = new string[map.Count];

			for (var i = 0; i < map.Count; i++)
			{
				fieldMap[i] = map[i].Item2;
			}

			// now we have the global object built
			// need to translate each method

			// first, build up a skeleton of methods
			var breakerMethods = new List<List<VarCode.Method>>();
			var skeletonMethods = new List<(VarCode.Method, Flattening.Method)>();

			foreach (var method in _flattened.Methods)
			{
				Log.Debug("Deobjectifying " + method.Name + ", @" + (method.Owner?.Name ?? "??"));
				var returnType = BuiltinType.ToObjectType(method.BoundMethod.ReturnType);
				var parameters = method.Parameters.Select(x => x.Type);

				if (method.Owner != null)
				{
					parameters = parameters.Prepend(ObjectType.Object);
				}

				var varMethod = new VarCode.Method(returnType, method.Name, parameters.ToList());
				skeletonMethods.Add((varMethod, method));

//============================================================================================================================== SKELETON METHODS (i keep not knowing where this is)

				if (method.Owner != null && !method.BoundMethod.IsConstructor)
				{
					var breakerName = ToBreakerName(varMethod, method.BoundMethod.Name);
					if (!breakerMethods.Any(x => x[0].Name == breakerName))
					{
						var breakerMethod = new VarCode.Method(varMethod.Returns, breakerName, new List<ObjectType>(varMethod.Parameters));

						// load all parameters into local variables
						for (var i = 0; i < varMethod.Parameters.Count; i++)
						{
							breakerMethod.Code.Add(new VarCode.Instruction.Load.Parameter(i + (breakerMethod.Returns == ObjectType.Void ? 3 : 4), i));
						}

						// field '0' should be the type
						breakerMethod.Code.Add(new VarCode.Instruction.GetField(0, (breakerMethod.Returns == ObjectType.Void ? 3 : 4), 0));

						breakerMethods.Add(new List<VarCode.Method>
					{
						breakerMethod,
						varMethod
					});

						AddComparison(breakerMethod, varMethod, classMap.IndexOf(method.Owner));
					}
					else
					{
						breakerMethods.First(x => x[0].Name == breakerName).Add(varMethod);
						AddComparison(breakerMethods.First(x => x[0].Name == breakerName)[0], varMethod, classMap.IndexOf(method.Owner));
					}

					void AddComparison(VarCode.Method breakerMethod, VarCode.Method sub, int type)
					{
						var args = new List<int>();

						var clause = new List<VarCode.Instruction>
						{
							new VarCode.Instruction.Call(breakerMethod.Returns == ObjectType.Void ? -1 : 4, sub, args)
						};

						if (breakerMethod.Returns != ObjectType.Void)
						{
							clause.Add(new VarCode.Instruction.Return(4));
						}
						else
						{
							clause.Add(new VarCode.Instruction.Return(-1));
						}

						for (var i = 0; i < breakerMethod.Parameters.Count; i++)
						{
							args.Add(i + (breakerMethod.Returns == ObjectType.Void ? 3 : 4));
						}

						breakerMethod.Code.Add(new VarCode.Instruction.Load.Number(1, new Number(type)));
						breakerMethod.Code.Add(new VarCode.Instruction.CompilerCall(2, _target.Match(TargetMethodNames.OperatorEqualTo, BuiltinType.Number, BuiltinType.Number), new List<int> { 0, 1 }));
						breakerMethod.Code.Add(new VarCode.Instruction.If(2, clause));
					}
				}

				static string ToBreakerName(VarCode.Method method, string name)
				{
					var strb = new StringBuilder();

					strb.Append('r');
					strb.Append(method.Returns.ToString());

					foreach (var i in method.Parameters)
					{
						strb.Append('p');
						strb.Append(i.ToString());
					}

					strb.Append('#');
					strb.Append(name);

					return strb.ToString();
				}
			}

			// then, re-interpret the code
			foreach (var (varMethod, method) in skeletonMethods)
			{
				var indv = new IndividualDeobjectifier(skeletonMethods, fieldMap, varMethod, method, _target, cls => classMap.IndexOf(cls), breakerMethods);
				indv.Go();

				newMethods.Add(varMethod);
			}

			newMethods.AddRange(breakerMethods.Select(x => x[0]));

			Log.StageEnd();
			objectFields = map.Count;
			return newMethods;
		}
	}

	public class IndividualDeobjectifier
	{
		public struct ScopeId
		{
			public ScopeId(int id) => Id = id;
			// TODO: do we even need anything in here?

			// TODO: stricter type checking?
			// public ObjectType Type;
			public int Id;
			public static implicit operator int(ScopeId scopeId) => scopeId.Id;
		}

		public class Scope
		{
			public Scope(Scope? previous = null)
			{
				Previous = previous;
			}

			public Scope? Previous { get; }
			public Dictionary<string, ScopeId> _defs = new Dictionary<string, ScopeId>();

			public ScopeId? Get(string varName)
			{
				if (_defs.TryGetValue(varName, out var scopeId))
				{
					return scopeId;
				}

				if (Previous == null)
				{
					return null;
				}

				return Previous.Get(varName);
			}

			public void Set(string varName, ScopeId scopeId)
			{
				_defs[varName] = scopeId;
			}
		}

		private readonly List<(VarCode.Method, Flattening.Method)> _skeletonMethods;

		private Scope _scope = new Scope();
		private List<VarCode.Instruction> _instructions = new List<VarCode.Instruction>();

		private int _this;
		private int _i;
		private const int _junk = VarCode.Instruction.Nowhere;
		private readonly VarCode.Method _varMethod;
		private readonly Flattening.Method _method;
		private readonly ICompilerTarget _target;
		private readonly string[] _fieldMap;
		private readonly int[] _fieldIds;
		private int[] _methodParams;
		private VarCode.Instruction[] _loadField;
		private readonly List<List<VarCode.Method>> _breakers;
		private readonly Func<Flattening.Class, int> _classToType;

		public IndividualDeobjectifier
		(
			List<(VarCode.Method, Flattening.Method)> skeletonMethods,
			string[] fieldMap,
			VarCode.Method varMethod,
			Flattening.Method method,
			ICompilerTarget target,
			Func<Flattening.Class, int> classToType,
			List<List<VarCode.Method>> breakers
		)
		{
			_skeletonMethods = skeletonMethods;
			_varMethod = varMethod;
			_method = method;
			_target = target;
			_fieldMap = fieldMap;
			_fieldIds = new int[_fieldMap.Length];
			_methodParams = new int[method.Parameters.Count];
			_loadField = new VarCode.Instruction[_fieldIds.Length];
			_breakers = breakers;
			_classToType = classToType;
		}

		private List<VarCode.Instruction> IncreaseScope()
		{
			var tmp = _instructions;
			_instructions = new List<VarCode.Instruction>();
			_scope = new Scope(_scope);
			return tmp;
		}

		private void DecreaseScope(List<VarCode.Instruction> repl)
		{
			_instructions = repl;
			_scope = _scope.Previous;
		}

		private int FieldIdOf(string field, bool load)
		{
			for (int i = 0; i < _fieldMap.Length; i++)
			{
				if (_fieldMap[i] == field)
				{
					if (_loadField[i] != null && load)
					{
						_instructions.Add(_loadField[i]);
					}

					return i;
				}
			}

			Log.Error($"Couldn't find field {field}");
			throw new InvalidOperationException();
		}

		public void Go()
		{
			if (_method.Owner != null)
			{
				_this = _i++;
				_scope.Set(Flattening.Scope.SGetThis(), new ScopeId(_this));

				_instructions.Add(new VarCode.Instruction.Load.Parameter(_this, 0));

				if (_method.Name.EndsWith("#ctor"))
				{
					var tmp = _i++;
					_instructions.Add(new VarCode.Instruction.Load.Number(tmp, new Number(_classToType(_method.Owner))));
					_instructions.Add(new VarCode.Instruction.SetField(0, 0, tmp));
				}

				foreach (var field in _method.Owner.Fields)
				{
					var fieldVarId = _i++;
					var fieldId = FieldIdOf(field.ToWeirdName(), false);
					_loadField[fieldId] = new VarCode.Instruction.GetField(fieldVarId, _this, fieldId);
					_fieldIds[fieldId] = fieldVarId;
				}
			}

			// set parameters, and offset it by 1 if this method belongs to a class
			int parameterId = _method.Owner != null ? 1 : 0;
			var c = 0;

			foreach(var p in _method.Parameters)
			{
				var pId = _i++;
				_instructions.Add(new VarCode.Instruction.Load.Parameter(pId, parameterId));

				_methodParams[c] = pId;
				_scope.Set(Flattening.Scope.SGetParameter(c++), new ScopeId(pId));

				parameterId++;
			}

			// _junk = _i++;
			Handle(_method.Body);

			_varMethod.Code.AddRange(_instructions);
		}

		private ScopeId ScopeGet(string name)
		{
			var result = _scope.Get(name);

			if (result == null)
			{
				result = new ScopeId(_i++);
				_scope.Set(name, (ScopeId)result);
			}

			return (ScopeId)result;
		}

		public void Handle(List<Flattening.Instruction> instructions)
		{
			foreach (var i in instructions)
			{
				switch (i)
				{
					case Instruction.Assignment o:
					{
						_instructions.Add(new VarCode.Instruction.Assign(ScopeGet(o.VariableName), ScopeGet(o.VariableValue)));
					}
					break;

					case Instruction.CompilerCall o:
					{
						int result = o.ResultVariable == null ? _junk : ScopeGet(o.ResultVariable);

						// TODO: resolve compiler calls
						var method = Match(o.Calling, o.Parameters.Count);
						_instructions.Add(new VarCode.Instruction.CompilerCall(result, method, o.Parameters.Select(ScopeGet).Select(x => (int)x).ToList()));

						if (o.ResultVariable != null)
						{
							_scope.Set(o.ResultVariable, new ScopeId { Id = result });
						}
					}
					break;

					case Instruction.Dereference o:
					{
						var fieldId = FieldIdOf(o.TargetFieldName, false);
						var varId = ScopeGet(o.TargetVariableName);
						var result = ScopeGet(o.ResultVariableName);
						_instructions.Add(new VarCode.Instruction.GetField(result, varId, fieldId));
					}
					break;

					case Instruction.If o:
					{
						var cmp = ScopeGet(o.ComparisonVariable);

						var tmp = IncreaseScope();
						Handle(o.TrueClause);
						var trueClause = _instructions;
						DecreaseScope(tmp);

						if (trueClause.Count > 0)
						{
							_instructions.Add(new VarCode.Instruction.If(cmp, trueClause));
						}

						var opposite = _i++;
						_instructions.Add(new VarCode.Instruction.CompilerCall(opposite, Match(TargetMethodNames.OperatorNot, 1), new List<int> { cmp }));

						tmp = IncreaseScope();
						Handle(o.ElseClause);
						var elseClause = _instructions;
						DecreaseScope(tmp);

						if (elseClause.Count > 0)
						{
							_instructions.Add(new VarCode.Instruction.If(opposite, elseClause));
						}
					}
					break;

					case Instruction.While o:
					{
						var cmp = ScopeGet(o.ComparisonVariable);

						var tmp = IncreaseScope();
						Handle(o.Body);
						var clause = _instructions;
						DecreaseScope(tmp);

						_instructions.Add(new VarCode.Instruction.While(cmp, clause));
					}
					break;

					case Instruction.LoadConstant o:
					{
						var target = ScopeGet(o.AssignTo);

						switch (o.ObjectValue)
						{
							case string s: _instructions.Add(new VarCode.Instruction.Load.String(target, s)); break;
							case Number n: _instructions.Add(new VarCode.Instruction.Load.Number(target, n)); break;
							case bool b: _instructions.Add(new VarCode.Instruction.Load.Boolean(target, b)); break;
							default: throw new NotImplementedException();
						}
					}
					break;

					case Instruction.MethodCall o:
					{
						var inst = o.Instance == null ? -1 : ScopeGet(o.Instance);
						var store = o.ResultVariable == null ? _junk : ScopeGet(o.ResultVariable);

						var args = o.Parameters.Select(x => (int)ScopeGet(x));

						if (inst != -1)
						{
							args = args.Prepend(inst);
						}

						var (targetMethod, calling) = _skeletonMethods.First(x => x.Item2 == o.Calling);

						if (inst != -1 && targetMethod.SimpleName != "ctor")
						{
							// call the breaker method  for classes
							targetMethod = _breakers.First(x => x[0].Returns == targetMethod.Returns
								&& x[0].Parameters.SequenceEqual(targetMethod.Parameters)
								&& x[0].SimpleName == targetMethod.SimpleName)[0];
						}

						_instructions.Add(new VarCode.Instruction.Call(store, targetMethod, args.ToList()));
					}
					break;

					case Instruction.New o:
					{
						_instructions.Add(new VarCode.Instruction.New(ScopeGet(o.AssignTo)));
					}
					break;

					case Instruction.Reference o:
					{
						_scope.Set(o.ResultVariableName, new ScopeId(_methodParams[o.MethodParameterIndex]));
					}
					break;

					case Instruction.Return o:
					{
						if (o.ReturnVariable == null)
						{
							_instructions.Add(new VarCode.Instruction.Return(-1));
						}
						else
						{
							_instructions.Add(new VarCode.Instruction.Return(ScopeGet(o.ReturnVariable)));
						}
					}
					break;

					case Instruction.SetField o:
					{
						var fieldId = FieldIdOf(o.TargetFieldName, false);
						var id = ScopeGet(o.NewValue);
						var target = ScopeGet(o.TargetVariableName);
						_instructions.Add(new VarCode.Instruction.SetField(target, fieldId, id));
					}
					break;
				}
			}
		}

		private CompilerMethod Match(string name, int paramCount)
		{
			var method = _target.Match(name, new IType[paramCount]);
			return method;
		}
	}
}
