using System;
using System.IO;
using Terumi.Ast;

namespace Terumi.Workspace
{
	public class SourceFile : IDisposable
	{
		private bool _disposed;

		public SourceFile(Stream source, PackageLevel packageLevel)
		{
			_source = source;
			_packageLevel = packageLevel;
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

		public void Dispose()
		{
			Source.Dispose();
			_disposed = true;
		}
	}
}