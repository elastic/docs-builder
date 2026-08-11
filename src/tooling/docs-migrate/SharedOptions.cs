// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.LegacyDocs.Migration;

namespace Documentation.Migrate;

internal sealed record FilterOptions(int Majors = 1, bool All = false, int? MinVersion = null, string? Book = null, int? Minors = null);

internal static class SharedOptions
{
	private const string CloneOptionsFile = ".clone-options.json";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public static readonly DirectoryInfo DefaultWorkDir = ResolveDefaultWorkDir();

	public static string ResolveWorkDir(string? workDir) =>
		workDir ?? DefaultWorkDir.FullName;

	private static DirectoryInfo ResolveDefaultWorkDir()
	{
		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		// Docker/CI containers often have no XDG_DATA_HOME or HOME set, causing
		// LocalApplicationData to return "". Fall back to the system temp directory.
		if (string.IsNullOrEmpty(localAppData))
			localAppData = Path.GetTempPath();
		return new DirectoryInfo(Path.Join(localAppData, "elastic", "docs-migrate"));
	}

	public static async Task<LegacyConf> LoadConfAsync(string workDir, CancellationToken ct)
	{
		var confPath = Path.Combine(workDir, "conf.yaml");
		if (!File.Exists(confPath))
			throw new FileNotFoundException($"conf.yaml not found at {confPath}. Run 'docs-migrate init' first.");

		var yaml = await File.ReadAllTextAsync(confPath, ct);
		var conf = LegacyConfParser.Parse(yaml);

		if (!conf.Repos.ContainsKey("docs"))
			conf.Repos["docs"] = "https://github.com/elastic/docs.git";

		return conf;
	}

	public static void SaveFilterOptions(string workDir, FilterOptions opts)
	{
		var path = Path.Combine(workDir, CloneOptionsFile);
		File.WriteAllText(path, JsonSerializer.Serialize(opts, JsonOptions));
	}

	public static FilterOptions LoadFilterOptions(string workDir)
	{
		var path = Path.Combine(workDir, CloneOptionsFile);
		if (!File.Exists(path))
			return new FilterOptions();

		var json = File.ReadAllText(path);
		return JsonSerializer.Deserialize<FilterOptions>(json, JsonOptions) ?? new FilterOptions();
	}

	public static FilterOptions ResolveFilterOptions(
		string workDir, int? majors, bool? all, int? minVersion, string? book, int? minors)
	{
		var saved = LoadFilterOptions(workDir);

		return new FilterOptions(
			Majors: majors ?? saved.Majors,
			All: all ?? saved.All,
			MinVersion: minVersion ?? saved.MinVersion,
			Book: book,
			Minors: minors ?? saved.Minors
		);
	}

	public static List<LegacyBook> FilterBooks(LegacyConf conf, string? bookFilter) =>
		conf.Contents
			.SelectMany(c => c.Sections)
			.Where(b => bookFilter is null || b.Prefix.StartsWith(bookFilter, StringComparison.OrdinalIgnoreCase))
			.ToList();

	public static List<BranchRef> FilterVersions(LegacyBook book, int majors, bool all, int? minVersion, int? minors = null)
	{
		var branches = book.Branches.ToList();

		if (minVersion is not null)
			branches = branches
				.Where(b => TryParseMajorMinor(b.VersionLabel) is var p && p.HasValue && p.Value.Major >= minVersion)
				.ToList();

		if (all)
			return EnsureCurrent(book, SortDescending(branches));

		var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		var grouped = branches
			.Select(b => (Branch: b, Parsed: TryParseMajorMinor(b.VersionLabel)))
			.Where(x => x.Parsed.HasValue)
			.GroupBy(x => x.Parsed!.Value.Major)
			.OrderByDescending(g => g.Key)
			.Take(majors);

		foreach (var group in grouped)
		{
			var minorsSorted = group.OrderByDescending(x => x.Parsed!.Value.Minor);
			var limited = minors is not null ? minorsSorted.Take(minors.Value) : minorsSorted;
			foreach (var (branch, _) in limited)
				_ = selected.Add(branch.VersionLabel);
		}

		var result = branches.Where(b => selected.Contains(b.VersionLabel)).ToList();
		return EnsureCurrent(book, SortDescending(result));
	}

	private static List<BranchRef> EnsureCurrent(LegacyBook book, List<BranchRef> versions)
	{
		if (string.IsNullOrEmpty(book.Current))
			return versions;

		if (versions.Any(v => v.VersionLabel == book.Current))
			return versions;

		var currentBranch = book.Branches.FirstOrDefault(b => b.VersionLabel == book.Current)
			?? new BranchRef(book.Current);

		versions.Insert(0, currentBranch);
		return versions;
	}

	private static List<BranchRef> SortDescending(IEnumerable<BranchRef> branches) =>
		branches
			.Select(b => (Branch: b, Parsed: TryParseMajorMinor(b.VersionLabel)))
			.OrderByDescending(x => x.Parsed?.Major ?? 0)
			.ThenByDescending(x => x.Parsed?.Minor ?? 0)
			.Select(x => x.Branch)
			.ToList();

	private static (int Major, int Minor)? TryParseMajorMinor(string version)
	{
		var parts = version.Split('.');
		if (parts.Length >= 2 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor))
			return (major, minor);
		return null;
	}
}
