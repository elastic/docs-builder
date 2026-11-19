// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using AwesomeAssertions;

namespace Elastic.Documentation.Configuration.Tests.Loading;

/// <summary>
/// A tolerant <c>Fatal</c> mode hides errors from a load's outcome, so each use must say what it preserves, what locks it and what the strict
/// behaviour would be (Constraints-Conventions.md, "Tolerant loads"). This guard is deliberately plain text: a use of <c>Fatal.CheckErrors</c> or
/// <c>Fatal.None</c> in a source file needs a <c>// TOLERANT LOAD:</c> comment on one of the <see cref="Window"/> lines above it. It does not judge the
/// comment's content; review does.
/// </summary>
public partial class TolerantLoadCommentTests
{
	private const int Window = 12;

	[GeneratedRegex(@"\bFatal\.(CheckErrors|None)\b", RegexOptions.None, 1000)]
	private static partial Regex NonDefault();

	private static IEnumerable<int> Unannotated(string source)
	{
		var lines = source.Split('\n');
		for (var i = 0; i < lines.Length; i++)
		{
			if (lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal) || !NonDefault().IsMatch(lines[i]))
				continue;
			var from = Math.Max(0, i - Window);
			if (!lines[from..(i + 1)].Any(l => l.Contains("// TOLERANT LOAD:", StringComparison.Ordinal)))
				yield return i + 1;
		}
	}

	[Test]
	public void Guard_FlagsAUseWithoutTheComment() => Unannotated("var l = Loader.For(p, m, Fatal.CheckErrors);").Should().Equal(1);

	[Test]
	public void Guard_AcceptsAUseWithTheCommentAbove() =>
		Unannotated("// TOLERANT LOAD: keeps x\nvar l = Loader.For(p, m, fatal: Fatal.None);").Should().BeEmpty();

	[Test]
	public void Guard_IgnoresTheStrictModeAndComments() =>
		Unannotated("var l = Loader.For(p, m, Fatal.All);\n// mentions Fatal.None only in prose").Should().BeEmpty();

	[Test]
	public void EverySourceUseOfANonDefaultMode_CarriesATolerantLoadComment()
	{
		var src = Path.Combine(RepositoryRoot(), "src");
		var offenders = Directory
			.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
			.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			.Where(f => Path.GetFileName(f) is not ("Loader.cs" or "MapContext.cs"))
			.SelectMany(f => Unannotated(File.ReadAllText(f)).Select(line => $"{Path.GetRelativePath(src, f)}:{line}"))
			.ToList();

		offenders.Should().BeEmpty("a non-default Fatal mode needs a '// TOLERANT LOAD:' comment (see Constraints-Conventions.md)");
	}

	private static string RepositoryRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docs-builder.slnx")))
			dir = dir.Parent;
		return dir?.FullName ?? throw new InvalidOperationException("docs-builder.slnx not found above the test binaries");
	}
}
