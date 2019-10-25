using Terumi.Binder;
using Terumi.ShellNeutral;

namespace Terumi.Ast.Translation
{
	public static class Translate
	{
		public static void Project(Writer writer, TypeInformation typeInformation)
		{
			// we need to translate everything

			// classes -> constructors which initialize fields into the negatives
			// classes with functions -> passing in fields of the class into the negative variabels
		}
	}
}