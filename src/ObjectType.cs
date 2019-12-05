using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi
{
	public enum ObjectType
	{
		Void,
		String,
		Number,
		Boolean,

		// blanket 'object' type, because in the deobjectification step
		// everything becomes a single object anyways
		Object,
	}
}
