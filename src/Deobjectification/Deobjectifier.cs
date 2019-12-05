using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.Flattening;

namespace Terumi.Deobjectification
{
	public class Deobjectifier
	{
		private readonly Binder.TerumiBinderBindings _bindings;
		private readonly FlattenedProject _flattened;

		public Deobjectifier(Binder.TerumiBinderBindings bindings, FlattenedProject flattened)
		{
			_bindings = bindings;
			_flattened = flattened;
		}

		public void Translate()
		{
			var newMethods = new List<VarCode.Method>();

			Log.Stage("DEOBJ", "Deobjectifying flattened code");

			// first, we're going to build a giant single global object
			var map = new List<(Flattening.ObjectType, string)>();

			foreach (var @class in _flattened.Classes)
			{
				foreach (var field in @class.Fields)
				{
					var item = (field.Type, field.Name);

					if (!map.Contains(item))
					{
						map.Add(item);
					}
				}
			}

			// now we have the global object built
			// need to translate each method

			/*
			foreach (var method in _flattened.Methods)
			{
				// nm = new method
				var nm = new VarCode.Method(null, method.BoundMethod, method.Name, method.);
				newMethods.Add(nm);

				if (method.Owner != null)
				{
					// we need to deobjectify this method
					// need to map all the parameters up one since this is an instance method,
					// and re-map all 
				}
			}
			*/

			Log.StageEnd();
		}
	}
}
