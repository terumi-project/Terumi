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

		public List<VarCode.Method> Translate()
		{
			var newMethods = new List<VarCode.Method>();

			Log.Stage("DEOBJ", "Deobjectifying flattened code");

			// first, we're going to build a giant single global object
			var map = new List<(ObjectType, string)>();

			foreach (var @class in _flattened.Classes)
			{
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
			var skeletonMethods = new List<(VarCode.Method, Flattening.Method)>();

			foreach (var method in _flattened.Methods)
			{
				var returnType = BuiltinType.ToObjectType(method.BoundMethod.ReturnType);
				var parameters = method.Parameters.Select(x => x.Type);

				if (method.Owner != null)
				{
					parameters = parameters.Prepend(ObjectType.Object);
				}

				var varMethod = new VarCode.Method(returnType, method.Name, parameters.ToList());
				skeletonMethods.Add((varMethod, method));
			}

			// then, re-interpret the code
			foreach (var (varMethod, method) in skeletonMethods)
			{
				var indv = new IndividualDeobjectifier(skeletonMethods, fieldMap, varMethod, method, _target);
				indv.Go();

				newMethods.Add(varMethod);
			}

			Log.StageEnd();

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
		private int _junk;
		private readonly VarCode.Method _varMethod;
		private readonly Flattening.Method _method;
		private readonly ICompilerTarget _target;
		private readonly string[] _fieldMap;
		private readonly int[] _fieldIds;
		private int[] _methodParams;

		public IndividualDeobjectifier
		(
			List<(VarCode.Method, Flattening.Method)> skeletonMethods,
			string[] fieldMap,
			VarCode.Method varMethod,
			Flattening.Method method,
			ICompilerTarget target
		)
		{
			_skeletonMethods = skeletonMethods;
			_varMethod = varMethod;
			_method = method;
			_target = target;
			_fieldMap = fieldMap;
			_fieldIds = new int[_fieldMap.Length];
			_methodParams = new int[method.Parameters.Count];
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

		private int FieldIdOf(string field)
		{
			for (int i = 0; i < _fieldMap.Length; i++)
			{
				if (_fieldMap[i] == field) return i;
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

				foreach (var field in _method.Owner.Fields)
				{
					var fieldVarId = _i++;
					var fieldId = FieldIdOf(field.ToWeirdName());
					_instructions.Add(new VarCode.Instruction.GetField(_this, fieldVarId, fieldId));
					_fieldIds[fieldId] = fieldVarId;
				}
			}

			// set parameters, and offset it by 1 if this method belongs to a class
			int parameterId = _method.Owner == null ? 1 : 0;
			var c = 0;

			foreach(var p in _method.Parameters)
			{
				var pId = _i++;
				_instructions.Add(new VarCode.Instruction.Load.Parameter(pId, parameterId));

				_methodParams[c] = pId;
				_scope.Set(Flattening.Scope.SGetParameter(c++), new ScopeId(pId));
			}

			_junk = _i++;
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
						var fieldId = _fieldIds[FieldIdOf(o.TargetFieldName)];
						var varId = ScopeGet(o.TargetVariableName);
						var result = _i++;
						_scope.Set(o.ResultVariableName, new ScopeId(result));
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

						_instructions.Add(new VarCode.Instruction.If(cmp, trueClause));

						var opposite = _i++;
						_instructions.Add(new VarCode.Instruction.CompilerCall(opposite, Match(TargetMethodNames.OperatorNot, 1), new List<int> { cmp }));

						tmp = IncreaseScope();
						Handle(o.ElseClause);
						var falseClause = _instructions;
						DecreaseScope(tmp);

						_instructions.Add(new VarCode.Instruction.If(opposite, falseClause));
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
						_instructions.Add(new VarCode.Instruction.Return(ScopeGet(o.ReturnVariable)));
					}
					break;

					case Instruction.SetField o:
					{
						var id = ScopeGet(o.NewValue);
						var target = ScopeGet(o.TargetVariableName);
						_instructions.Add(new VarCode.Instruction.GetField(id, target, FieldIdOf(o.TargetFieldName)));
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
