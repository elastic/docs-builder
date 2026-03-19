// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.LegacyDocs.Migration;

public record LegacyConf
{
	public Dictionary<string, LegacyRepo> Repos { get; init; } = [];
	public List<LegacyCategory> Contents { get; init; } = [];
}

public record LegacyRepo
{
	public string Url { get; init; } = "";
}

public record LegacyCategory
{
	public string Title { get; init; } = "";
	public List<LegacyBook> Sections { get; init; } = [];
}

public record LegacyBook
{
	public string Title { get; init; } = "";
	public string Prefix { get; init; } = "";
	public string Index { get; init; } = "";
	public string Current { get; init; } = "";
	public List<BranchRef> Branches { get; init; } = [];
	public List<string> Live { get; init; } = [];
	public int Chunk { get; init; } = 1;
	public string? Tags { get; init; }
	public string? Subject { get; init; }
	public List<LegacySource> Sources { get; init; } = [];
}

public record LegacySource
{
	public string Repo { get; init; } = "";
	public string Path { get; init; } = "";
	public List<BranchRef> ExcludeBranches { get; init; } = [];
	public Dictionary<string, string> MapBranches { get; init; } = [];
}

public record BranchRef(string Name, string? Alias = null);
