﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.Binder;

namespace Terumi.Ast
{
	public class CompilerEntity : ICodeExpression
	{
		private enum TType
		{
			String, Boolean, Number, Void
		}

		private struct Func
		{
			public string Name;
			public TType Returns;
			public TType[] Params;
		}

		private static IEnumerable<Func> Gen()
		{
			yield return new Func
			{
				Name = "println",
				Returns = TType.Void,
				Params = new[] { TType.String }
			};

			yield return new Func
			{
				Name = "println",
				Returns = TType.Void,
				Params = new[] { TType.Number }
			};
		}

		private static InfoItem Conv(TType type)
		{
			switch (type)
			{
				case TType.Boolean: return TypeInformation.Boolean;
				case TType.Number: return TypeInformation.Number;
				case TType.String: return TypeInformation.String;
				case TType.Void: return TypeInformation.Void;
			}

			throw new Exception();
		}

		public static ICodeExpression MatchMethod(string name, IEnumerable<InfoItem> parameters)
		{
			foreach(var i in Gen())
			{
				if (i.Name != name)
				{
					continue;
				}

				int j = 0;
				bool f = false;
				foreach(var p in parameters)
				{
					if (p != Conv(i.Params[j++]))
					{
						f = true;
						break;
					}
				}

				if (f)
				{
					continue;
				}

				int k = 0;
				return new CompilerEntity(new InfoItem
				{
					Code = new InfoItem.Method
					{
						Name = i.Name,
						Parameters = i.Params.Select(x => new InfoItem.Method.Parameter
						{
							Name = "k" + k++,
							Type = Conv(x)
						}).ToList()
					}
				});
			}

			throw new Exception("No matching compiler call");
		}

		public CompilerEntity(InfoItem infoItem)
		{
			Type = infoItem;
		}

		public InfoItem Type { get; }
	}
}