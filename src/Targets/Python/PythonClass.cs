using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terumi.Targets.Python
{
	public static class PythonClass
	{
		public static void Write
		(
			Stream destination,
			string name,
			string[] fields
		)
		{
			using (var w = new StreamWriter(destination, null, -1, true))
			{
				w.WriteLine("def " + name + "():");

				foreach(var field in fields)
				{
					w.WriteLine("\tthis." + field + " = None");
				}
			}
		}
	}
}
