// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json.Serialization;
using IniParser;
using IniParser.Model;

namespace Elastic.Markdown.IO;

public record GitConfiguration
{
	[JsonPropertyName("branch")] public required string Branch { get; init; }
	[JsonPropertyName("remote")] public required string Remote { get; init; }
	[JsonPropertyName("ref")] public required string Ref { get; init; }

	// manual read because libgit2sharp is not yet AOT ready
	public static GitConfiguration Create(IFileSystem fileSystem)
	{
		// filesystem is not real so return a dummy
		if (fileSystem is not FileSystem)
		{
			var fakeRef = Guid.NewGuid().ToString().Substring(0, 16);
			return new GitConfiguration { Branch = $"test-{fakeRef}", Remote = "elastic/docs-builder", Ref = fakeRef, };
		}

		var gitConfig = Git(".git/config");
		if (!gitConfig.Exists)
			throw new Exception($"{Paths.Root.FullName} is not a git repository.");

		var head = Read(".git/HEAD").Replace("ref: ", string.Empty);
		var gitRef = Read(".git/" + head);
		var branch = head.Replace("refs/heads/", string.Empty);

		var ini = new FileIniDataParser();
		using var stream = gitConfig.OpenRead();
		using var streamReader = new StreamReader(stream);
		var config = ini.ReadData(streamReader);
		var remote = BranchTrackingRemote(branch, config);
		if (string.IsNullOrEmpty(remote))
			remote = BranchTrackingRemote("main", config);
		if (string.IsNullOrEmpty(remote))
			remote = BranchTrackingRemote("master", config);


		return new GitConfiguration { Ref = gitRef, Branch = branch, Remote = remote };

		IFileInfo Git(string path) => fileSystem.FileInfo.New(Path.Combine(Paths.Root.FullName, path));

		string Read(string path) =>
			fileSystem.File.ReadAllText(Git(path).FullName).Trim(Environment.NewLine.ToCharArray());

		string BranchTrackingRemote(string b, IniData c)
		{
			var remoteName = c[$"branch \"{b}\""]["remote"];
			remote = c[$"remote \"{remoteName}\""]["url"];
			return remote;
		}
	}
}
