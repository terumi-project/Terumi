using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Terumi.Flattening
{
	public struct FlattenedProject
	{
		public List<Class> Classes;
		public List<Method> Methods;
	}

	public struct Maps
	{
		public struct Bind<T>
		{
			T Item;
			Binder.BoundFile BoundIn;

			public void Deconstruct(out T item, out Binder.BoundFile boundIn)
			{
				item = Item;
				boundIn = BoundIn;
			}

			public static implicit operator Bind<T>((T Item, Binder.BoundFile File) tuple) => new Bind<T>
			{
				Item = tuple.Item,
				BoundIn = tuple.File
			};
		}

		public Dictionary<Binder.Class, Bind<Class>> ClassMap;
		public Dictionary<Binder.Method, Bind<Method>> MethodMap;
		public List<Method> Codegenned;
	}

	public class Flattener
	{
		private readonly Binder.TerumiBinderBindings _bindings;

		public Flattener(Binder.TerumiBinderBindings bindings)
		{
			_bindings = bindings;
		}

		public FlattenedProject Flatten()
		{
			Log.Stage("FLAT", "Flattening project");

			var project = new FlattenedProject
			{
				Classes = new List<Class>(),
				Methods = new List<Method>()
			};

			// "skeletonize" everything - examine each class and method and produce a given class or method

			var maps = new Maps
			{
				ClassMap = new Dictionary<Binder.Class, Maps.Bind<Class>>(),
				MethodMap = new Dictionary<Binder.Method, Maps.Bind<Method>>(),
				Codegenned = new List<Method>()
			};

			foreach (var file in _bindings.BoundProjectFiles
				.Concat(_bindings.DirectDependencies)
				.Concat(_bindings.IndirectDependencies))
			{
				var unique = CalcUnique(file);

				static string CalcUnique(Binder.BoundFile file)
				{
					return $"{file.FilePath};{file.Namespace};".Hash();
				}

				foreach (var @class in file.Classes)
				{
					var skeleton = new Class(GetName(unique, file, @class, null), @class);
					maps.ClassMap[@class] = (skeleton, file);

					foreach (var method in @class.Methods)
					{
						var classMethod = MapMethod(method as Binder.Method, @class, skeleton);
						maps.MethodMap[method as Binder.Method] = (classMethod, file);
					}
				}

				foreach (var method in file.Methods)
				{
					maps.MethodMap[method] = (MapMethod(method, null, null), file);
				}

				Method MapMethod(Binder.Method method, Binder.Class? context, Class? owner)
				{
					var skeleton = new Method(GetName(unique, file, context, method), owner);

					foreach (var parameter in method.Parameters)
					{
						if (parameter.Type == Binder.BuiltinType.Void) throw new InvalidOperationException();
						else if (parameter.Type == Binder.BuiltinType.String) skeleton.Parameters.Add(new TypedPair(Type.String, parameter.Name));
						else if (parameter.Type == Binder.BuiltinType.Number) skeleton.Parameters.Add(new TypedPair(Type.Number, parameter.Name));
						else if (parameter.Type == Binder.BuiltinType.Boolean) skeleton.Parameters.Add(new TypedPair(Type.Boolean, parameter.Name));
						else skeleton.Parameters.Add(new TypedPair(Type.Object, parameter.Name));
					}

					return skeleton;
				}
			}

			Log.Info("Created skeletons, doing codegen");
			Codegen(maps);
			Log.Info("Did codegen, translating method bodies");

			Translate(maps);

			foreach (var (_, (@class, _)) in maps.ClassMap)
			{
				project.Classes.Add(@class);
			}

			foreach (var (_, (method, _)) in maps.MethodMap)
			{
				project.Methods.Add(method);
			}

			Log.StageEnd();
			return project;
		}

		public void Translate(Maps maps)
		{
			foreach (var (boundMethod, (method, file)) in maps.MethodMap)
			{
				if (method.Owner != null)
				{
					var translator = new InstructionTranslator(boundMethod, method.Body, new Scope(maps));
					translator.Run();
				}
			}
		}

		public void Codegen(Maps maps)
		{
			BlankConstructorGen(maps);
		}

		public void BlankConstructorGen(Maps maps)
		{
			foreach (var (boundClass, (@class, file)) in maps.ClassMap)
			{
				var hasCtor = boundClass.Methods.Any(x => x.Name == Binder.Constants.ConstructorMethodName);

				if (!hasCtor)
				{
					var ctorName = GetName(null, file, boundClass.Name, Binder.Constants.ConstructorMethodName);
					var ctorMethod = new Method(ctorName, @class);
					maps.Codegenned.Add(ctorMethod);

					// TODO: set fields
					ctorMethod.Body.Add(new Instruction.LoadConstant("<>_msg", "Codegen for empty constructors currently not available at this time. " +
						"Please give this class an empty constructor."));
					ctorMethod.Body.Add(new Instruction.CompilerCall(null, new List<string> { "<>_msg" }, "panic"));
				}
			}
		}

		public string GetName(string? uniqueCache, Binder.BoundFile file, Binder.Class? context = null, Binder.Method? method = null)
			=> GetName(uniqueCache, file, context?.Name, method?.Name);

		public string GetName(string? uniqueCache, Binder.BoundFile file, string? className = null, string? methodName = null)
		{
			string unique;
#if DEBUG
			// if we're debugging assert a couple things to make sure the wrong things are never passed in:
			// 1: either context or method has a value
			Debug.Assert(className != null || methodName != null);

			// 2: the uniqueCache is equivalent to the unique
			unique = $"{file.FilePath};{file.Namespace};".Hash();

			if (uniqueCache != null)
			{
				Debug.Assert(uniqueCache == unique);
			}
#endif
			unique = uniqueCache ?? $"{file.FilePath};{file.Namespace};".Hash();

			if (methodName == null)
			{
				// context isn't null - we're generating a class name
				var name = $"[{unique}]#{className}";
				return name;
			}
			else if (className == null)
			{
				// method isn't null - we're generating a global method
				var name = $"[{unique}]##{methodName}";
				return name;
			}
			else
			{
				// we're generating a method within a class
				var name = $"[{unique}]#{className}#{methodName}";
				return name;
			}
		}
	}
}
