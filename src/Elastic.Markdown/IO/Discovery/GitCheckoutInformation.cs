// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json.Serialization;
using SoftCircuits.IniFileParser;

namespace Elastic.Markdown.IO.Discovery;

public record GitCheckoutInformation
{
	private string? _repositoryName;

	private static GitCheckoutInformation Unavailable { get; } = new()
	{
		Branch = "unavailable",
		Remote = "unavailable",
		Ref = "unavailable",
		RepositoryName = "unavailable"
	};

	[JsonPropertyName("branch")]
	public required string Branch { get; init; }

	[JsonPropertyName("remote")]
	public required string Remote { get; init; }

	[JsonPropertyName("ref")]
	public required string Ref { get; init; }

	[JsonIgnore]
	public string? RepositoryName
	{
		get => _repositoryName ??= Remote.Split('/').Last();
		init => _repositoryName = value;
	}

	// manual read because libgit2sharp is not yet AOT ready
	public static GitCheckoutInformation Create(IDirectoryInfo source, IFileSystem fileSystem)
	{
		if (fileSystem is not FileSystem)
		{
			return new GitCheckoutInformation
			{
				Branch = $"test-e35fcb27-5f60-4e",
				Remote = "elastic/docs-builder",
				Ref = "e35fcb27-5f60-4e",
				RepositoryName = "docs-builder"
			};
		}

		var fakeRef = Guid.NewGuid().ToString()[..16];
		var gitConfig = Git(source, ".git/config");
		if (!gitConfig.Exists)
			return Unavailable;

		var head = Read(source, ".git/HEAD") ?? fakeRef;
		var gitRef = head;
		var branch = head.Replace("refs/heads/", string.Empty);
		//not detached HEAD
		if (head.StartsWith("ref:"))
		{
			head = head.Replace("ref: ", string.Empty);
			gitRef = Read(source, ".git/" + head) ?? fakeRef;
			branch = branch.Replace("ref: ", string.Empty);
		}
		else
			branch = "detached/head";

		var ini = new IniFile();
		using var stream = gitConfig.OpenRead();
		using var streamReader = new StreamReader(stream);
		ini.Load(streamReader);

		var remote = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
		if (string.IsNullOrEmpty(remote))
			remote = BranchTrackingRemote(branch, ini);
		if (string.IsNullOrEmpty(remote))
			remote = BranchTrackingRemote("main", ini);
		if (string.IsNullOrEmpty(remote))
			remote = BranchTrackingRemote("master", ini);
		if (string.IsNullOrEmpty(remote))
			remote = "elastic/docs-builder-unknown";

		remote = remote.AsSpan().TrimEnd("git").TrimEnd('.').ToString();

		return new GitCheckoutInformation
		{
			Ref = gitRef,
			Branch = branch,
			Remote = remote,
			RepositoryName = remote.Split('/').Last()
		};

		IFileInfo Git(IDirectoryInfo directoryInfo, string path) =>
			fileSystem.FileInfo.New(Path.Combine(directoryInfo.FullName, path));

		string? Read(IDirectoryInfo directoryInfo, string path)
		{
			var gitPath = Git(directoryInfo, path).FullName;
			return !fileSystem.File.Exists(gitPath)
				? null
				: fileSystem.File.ReadAllText(gitPath).Trim(Environment.NewLine.ToCharArray());
		}

		string BranchTrackingRemote(string b, IniFile c)
		{
			var sections = c.GetSections();
			var branchSection = $"branch \"{b}\"";
			if (!sections.Contains(branchSection))
				return string.Empty;

			var remoteName = ini.GetSetting(branchSection, "remote")?.Trim();

			var remoteSection = $"remote \"{remoteName}\"";

			remote = ini.GetSetting(remoteSection, "url");
			return remote ?? string.Empty;
		}
	}
}
