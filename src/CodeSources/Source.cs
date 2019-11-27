using Nett;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Terumi.CodeSources
{
	public class Source
	{
		public static Source Instance { get; } = new Source
		(
			package => new Uri($"https://raw.githubusercontent.com/terumi-project/packages/master/packages/{package}.toml")
		);

		private readonly Func<string, Uri> _packageToUrl;

		public Source(Func<string, Uri> packageToUrl)
		{
			_packageToUrl = packageToUrl;
		}

		public async Task<ToolInfo?> Fetch(string packageName)
		{
			var uri = _packageToUrl(packageName);

			// https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
			// this is bad, but it's ok because the compiler is short lived
			using (var client = new HttpClient())
			{
				Log.Info($"Making a request to {uri}");
				var result = await client.GetAsync(uri);

				if (!result.IsSuccessStatusCode)
				{
					Log.Warn($"Request failed");
					return null;
				}

				var data = await result.Content.ReadAsStreamAsync();

				return Toml.ReadStream<ToolInfo>(data);
			}
		}
	}

	public class ToolInfo
	{
		[TomlMember(Key = "name")]
		public string Name { get; set; }

		[TomlMember(Key = "author")]
		public string Author { get; set; }

		[TomlMember(Key = "description")]
		public string Description { get; set; }

		[TomlMember(Key = "snapshot")]
		public ToolSnapshot[] Snapshots { get; set; }
	}

	public class ToolSnapshot
	{
		[TomlMember(Key = "path")]
		public string Path { get; set; }

		[TomlMember(Key = "version")]
		public string Version { get; set; }

		[TomlMember(Key = "git_url")]
		public string GitUrl { get; set; }

		[TomlMember(Key = "branch")]
		public string Branch { get; set; }

		[TomlMember(Key = "commit")]
		public string CommitId { get; set; }
	}
}
