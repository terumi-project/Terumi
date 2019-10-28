using System.Linq;
using System.Numerics;
using Terumi.Binder;
using Terumi.ShellNeutral;

namespace Terumi.ShellNeutral
{
	public static class Translate
	{
		public static void Project(Writer writer, TypeInformation typeInformation)
		{
			// first, let's translate the type information into the translation layer

			var layer = new TranslationLayer
			{
				BackingInformation = typeInformation
			};

			// translate all methods

			BigInteger id = 1;

			foreach(var method in typeInformation.InfoItems)
			{
				if (true)
				{
					System.Console.WriteLine($"Cannot translate method {method.Name} - it's not a top level method.");
					continue;
				}

				BigInteger methodId = 0;

				// we should've verified from before that there can't be two methods with the same level
				if (method.Name != "main")
				{
					methodId = id++;
				}

				var translatedMethod = new TranslationLayer.Method
				{
					MethodName = method.Name,
					LabelId = methodId,
					BackingMethod = method.Methods.ElementAt(0)
				};

				BigInteger parameterId = 0;

				foreach(var parameter in method.Methods.ElementAt(0).Parameters)
				{
					translatedMethod.Parameters.Add(new TranslationLayer.Method.Parameter
					{
						ParameterName = parameter.Name,
						ParameterId = parameterId++,
						BackingParameter = parameter
					});
				}

				layer.Methods.Add(translatedMethod);
			}

			Project(writer, layer);
		}

		public static void Project(Writer writer, TranslationLayer translationLayer)
		{
			foreach(var method in translationLayer.Methods)
			{
				writer.Place(method.LabelId);

				writer.Pop();
			}
		}
	}
}