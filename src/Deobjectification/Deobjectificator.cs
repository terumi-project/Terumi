using System.Collections.Generic;
using Terumi.Binder;

using DMethod = Terumi.Deobjectification.Method;
using BMethod = Terumi.Binder.IMethod;
using System;
using System.Text;
using System.Linq;
using Terumi.Targets;

namespace Terumi.Deobjectification
{
	/// <summary>
	/// Translates an entire terumi project into a series of deobjectified methods
	/// </summary>
	public class Deobjectificator
	{
		private Random _rng = new Random();
		private readonly TerumiBinderBindings _bindings;

		public Deobjectificator(TerumiBinderBindings bindings)
		{
			_bindings = bindings;
		}

		// ooh boy
		// TODO: cleanup, i was tired... again...
		public List<DMethod> Translate()
		{
			var globalObject = new GlobalObjectInfo();

			// first, we want to generate a bunch of method skeletons
			var methods = new List<(DMethod, BMethod, Class?)>();
			var needsBreaking = new List<(Class, BMethod, DMethod)>();

			Log.Stage("DEOBJ", "Skeleton translation");

			foreach (var file in _bindings.IndirectDependencies
				.Concat(_bindings.DirectDependencies)
				.Concat(_bindings.BoundProjectFiles))
			{
				foreach (var @class in file.Classes)
				{
					// associate the class with a type
					globalObject.Types[@class] = GetUniqueThing(file, @class, null);

					// FIRST: if the class doesn't have a ctor, generate one
					var anyCtors = @class.Methods.Any(x => x.Name == "ctor");
					if (!anyCtors)
					{
						var unique = GetUniqueThing(file, @class, null);

						var parameters = new List<ObjectType> { ObjectType.GlobalObject };

						var dMethod = new DMethod(unique, $"<{@class.Name}>_ctor_", ObjectType.Void, parameters, @class.Name);

						var bMethod = new Binder.Method(null, BuiltinType.Void, "ctor")
						{
							Body = new CodeBody(new List<Statement>
							{
								// TODO: generate code to set all fields to default values
								// for now, don't
							})
						};

						methods.Add((dMethod, bMethod, @class));

						needsBreaking.Add((@class, bMethod, dMethod));
					}

					// generate a skeleton per class method
					foreach (var method in @class.Methods)
					{
						var unique = GetUniqueThing(file, @class, method);

						var parameters = new List<ObjectType> { ObjectType.GlobalObject };

						foreach (var parameter in method.Parameters)
						{
							parameters.Add(ToType(parameter.Type));
						}

						var dMethod = new DMethod(unique, $"<{@class.Name}>_{method.Name}_", ToType(method.ReturnType), parameters, @class.Name);
						methods.Add((dMethod, method, @class));

						needsBreaking.Add((@class, method, dMethod));
					}
				}

				foreach (var method in file.Methods)
				{
					var unique = GetUniqueThing(file, null, method);

					var parameters = new List<ObjectType>();

					foreach (var parameter in method.Parameters)
					{
						parameters.Add(ToType(parameter.Type));
					}

					methods.Add((new DMethod(unique, $"<>_{method.Name}_", ToType(method.ReturnType), parameters), method, null));
				}
			}

			Log.Info("Constructing global object");

			// we have to take each field and dump it down into a single global object that represents all the fields
			// for now, we're going to be very naive about it
			// just map a type and name to a slot id

			// FIRST: get all classes
			var classes = _bindings.IndirectDependencies
				.Concat(_bindings.DirectDependencies)
				.Concat(_bindings.BoundProjectFiles)
				.SelectMany(x => x.Classes);

			int freeSlot = 0;
			var slots = new Dictionary<(ObjectType, string), int>();

			foreach (var field in classes.SelectMany(x => x.Fields))
			{
				var key = (ToType(field.Type), field.Name);

				if (!slots.TryGetValue(key, out _))
				{
					slots[key] = freeSlot++;
				}
			}

			// then, map each field to one in the global info context

			// used to determine an object's type for method breakdown
			globalObject.Fields[DeobjectificationConstants.GlobalObjectType] = ObjectType.String;

			foreach (var ((type, name), _) in slots)
			{
				globalObject.Fields[$"<{type}>_{name}_"] = type;
			}

			// method breakers are methods that will break down a method into it's implementation based on a global object's type
			Log.Info("Generating method breakers");

			// first, sort all the methods in "needs skeletoning" based upon their return type and parameter types
			var buckets = new Dictionary<(ObjectType returnType, List<ObjectType> parameters), List<(Class, BMethod, DMethod)>>();

			foreach (var check in needsBreaking)
			{
				var unique = (ToType(check.Item2.ReturnType), check.Item2.Parameters.Select(x => ToType(x.Type)).ToList());

				if (buckets.TryGetValue(unique, out var breakers))
				{
					breakers.Add(check);
				}
				else
				{
					buckets[unique] = new List<(Class, BMethod, DMethod)> { check };
				}
			}

			var breakdowns = new List<DMethod>();

			// now that they're in buckets, break down each bucket
			foreach (var ((returnType, parameters), roads) in buckets)
			{
				var uniqueThing = GetUniqueThing(roads);

				// BREAKER<>asioas(ObjectType obj)
				var breakdown = new DMethod(GetUniqueThing(roads), $"BREAKER<>{uniqueThing}", returnType, parameters, null);

				// {
				var breakdownCode = new List<Instruction>();

				// _<>BREAKER_TYPE_TMP = obj.type
				var obj = new Instruction.Reference(null, DeobjectificationConstants.MethodParameter(breakdown, 0));
				var objType = new Instruction.Reference(obj, DeobjectificationConstants.GlobalObjectType);
				var typeVarName = DeobjectificationConstants.MethodVariableName(breakdown, "_<>BREAKER_TYPE_TMP_");
				breakdownCode.Add(new Instruction.Assignment(typeVarName, objType));

				var type = new Instruction.Reference(null, typeVarName);

				foreach (var (@class, method, dMethod) in roads)
				{
					var classType = globalObject.Types[@class];

					var eqParams = new List<Instruction> { type, new Instruction.Constant(classType) };
					var areEq = new Instruction.CompilerCall(TargetMethodNames.OperatorEqualTo, eqParams);
					var clause = new List<Instruction>();

					// if (_<>BREAKER_TYPE_TMP == "some predefined type")
					var @if = new Instruction.If(areEq, clause);
					breakdownCode.Add(@if);

					var args = new List<Instruction>();
					for (var i = 0; i < parameters.Count; i++)
					{
						args.Add(new Instruction.Reference(null, DeobjectificationConstants.MethodParameter(breakdown, i)));
					}
					var call = new Instruction.MethodCall(dMethod, args);

					// {
					if (returnType == ObjectType.Void)
					{
						// the_right_method(obj, ...)
						clause.Add(call);

						// return
						clause.Add(new Instruction.Return());
					}
					else
					{
						clause.Add(new Instruction.Return(call));
					}
					// }
				}

				// by this point no right method would've been found
				// call @panic
				breakdownCode.Add(new Instruction.CompilerCall(TargetMethodNames.Panic, new List<Instruction>
				{
					new Instruction.Constant($"[terumi] runtime error: couldn't find correct overload //TODO: more detail ")
				}));

				// return just incase compiler ignores panic for some reason
				breakdownCode.Add(new Instruction.Return());

				breakdown.Instructions.AddRange(breakdownCode);
				breakdowns.Add(breakdown);
			}

			Log.Stage("DEOBJ", "Translating code");

			// TODO: translate code

			// globalObject.Fields - maintains a key/value pair map of the global object's fields names and values
			//     <ObjectType>_name_ = ObjectType
			// globalObject.Types - maps a Binder.Class to a string representation of the type name
			// methods - all the methods that need translation
			// for class method calls, call the appropriate method breakdown (except for ctors, call ctors directly)

			// when i'm not tired i'll try to write cleaner code so i don't have to rewrite that at least lolo

			var store = new StoreGateway(globalObject, methods);

			foreach (var (method, bMethod, classCtx) in methods)
			{
				if (classCtx != null)
				{
					var translator = new CodeTranslator(method, classCtx, bMethod, store);
					translator.Transcribe();
				}
				else
				{
					// TODO: support non class methods
				}
			}

			Log.StageEnd();

			return methods.Select(x => x.Item1).Concat(breakdowns).ToList();
		}

		public ObjectType ToType(IType binderType)
		{
			if (binderType == BuiltinType.Void) return ObjectType.Void;
			else if (binderType == BuiltinType.String) return ObjectType.String;
			else if (binderType == BuiltinType.Number) return ObjectType.Number;
			else if (binderType == BuiltinType.Boolean) return ObjectType.Boolean;
			else return ObjectType.GlobalObject;
		}

		public string GetUniqueThing(List<(Class, BMethod, DMethod)> methods)
		{
			var uniqueness = new StringBuilder();

			foreach (var (@class, method, _) in methods)
			{
				uniqueness.Append(GetUniqueThing(null, @class, method));
			}

			return uniqueness.ToString().Hash();
		}

		public string GetUniqueThing(BoundFile? boundFile, Class? context, BMethod? method)
		{
			// the "unique thing" of a given method must be VERY unique, otherwise pretty bad compilation errors could occur
			// so we're just going to try include a bunch of information that may be relevant

			var uniqueness = new StringBuilder();

			// add rng to the uniqueness for extra touch
			// if we're debugging we want less randomness so that we can debug possible collisions
#if !DEBUG
			uniqueness
				.Append(_rng.Next())
				.Append('_')
				.Append(_rng.Next())
				.Append('_');
#endif

			uniqueness.Append("#BOUNDFILE");

			if (boundFile != null)
			{
				uniqueness
					.Append('<')
					.Append(boundFile.FilePath)
					.Append('>');
			}

			uniqueness.Append("#CLASS");

			if (context != null)
			{
				uniqueness
						.Append('<')
						.Append(context.Name)
						.Append('>')
					.Append('_')
					.Append(context.Fields.Count)
					.Append('_')
					.Append(context.Methods.Count)
					.Append('_');

				foreach (var field in context.Fields)
				{
					uniqueness
						.Append(field.Type.TypeName)
						.Append('_')
						.Append(field.Name)
						.Append('_');
				}

				foreach (var m in context.Methods)
				{
					uniqueness
						.Append(m.Name)
						.Append('_')
						.Append(m.ReturnType.TypeName)
						.Append('_')
						.Append(m.Parameters.Count)
						.Append('_');

					foreach (var p in m.Parameters)
					{
						uniqueness
							.Append(p.Type.TypeName)
							.Append('_')
							.Append(p.Name)
							.Append('_');
					}
				}
			}

			uniqueness.Append("#METHOD");

			if (method != null)
			{
				uniqueness
					.Append('<')
					.Append(method.Name)
					.Append('>');

				foreach (var p in method.Parameters)
				{
					uniqueness
						.Append(p.Type.TypeName)
						.Append('_')
						.Append(p.Name)
						.Append('_');
				}
			}

			return uniqueness.ToString().Hash();
		}
	}
}
