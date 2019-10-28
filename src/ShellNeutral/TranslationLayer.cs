using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Terumi.Binder;

namespace Terumi.ShellNeutral
{
	public class TranslationLayer
	{
		public TypeInformation BackingInformation { get; set; }

		public List<Method> Methods { get; set; } = new List<Method>();

		public class Method
		{
			public BigInteger LabelId { get; set; }

			public string MethodName { get; set; }

			public InfoItem.Method BackingMethod { get; set; }

			public List<Parameter> Parameters { get; set; } = new List<Parameter>();

			public class Parameter
			{
				public BigInteger ParameterId { get; set; }

				public string ParameterName { get; set; }

				public InfoItem.Method.Parameter BackingParameter { get; set; }
			}
		}
	}
}
