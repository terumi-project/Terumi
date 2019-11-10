using Terumi.Binder;

namespace Terumi.Targets
{
	public interface ICompilerMethods
	{
		ICompilerTarget MakeTarget(TypeInformation typeInformation);

		string println_string(string value);
		string println_number(string value);
		string println_bool(string value);
		string concat_string_string(string a, string b);
		string add_number_number(string a, string b);
	}
}
