using System;
using System.IO;

using Terumi.Ast;

namespace Terumi.Workspace
{
	public class SourceFile : IDisposable
	{
		private bool _disposed;

		public SourceFile(Stream source, PackageLevel packageLevel, string location)
		{
			_source = source;
			_packageLevel = packageLevel;
			Location = location;
		}

		private readonly Stream _source;

		public Stream Source
		{
			get
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(nameof(SourceFile));
				}

				return _source;
			}
		}

		private readonly PackageLevel _packageLevel;

		public PackageLevel PackageLevel
		{
			get
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(nameof(PackageLevel));
				}

				return _packageLevel;
			}
		}

		public string Location { get; }

		public void Dispose()
		{
			Source.Dispose();
			_disposed = true;
		}
	}
}