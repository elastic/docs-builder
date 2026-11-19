// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using AwesomeAssertions;

namespace Elastic.Documentation.Configuration.Tests.Loading;

/// <summary>
/// A tolerant <c>Fatal</c> mode hides errors from a load's outcome, so each use must say what it preserves, what locks it and what the strict
/// behaviour would be (Constraints-Conventions.md, "Tolerant loads"). This guard is deliberately plain text: every declaration that selects a tolerant
/// mode, by <c>Fatal.CheckErrors</c>, <c>Fatal.None</c> or <c>errorsAreFatal: false</c>, needs a <c>// TOLERANT LOAD:</c> comment in the comment block directly
/// above its own statement, so one annotation covers one declaration. It does not judge the comment's content; review does.
/// </summary>
public partial class TolerantLoadCommentTests
{
	private const string Marker = "// TOLERANT LOAD:";

	[GeneratedRegex(@"\bFatal\.(CheckErrors|None)\b|\berrorsAreFatal\s*:\s*false\b", RegexOptions.None, 1000)]
	private static partial Regex NonDefault();

	private static bool IsComment(string line) => line.TrimStart().StartsWith("//", StringComparison.Ordinal);

	private static bool EndsStatement(string line)
	{
		var t = line.TrimEnd();
		return t.Length == 0 || t.EndsWith(';') || t.EndsWith('{') || t.EndsWith('}') || IsComment(t);
	}

	// Brackets closed minus opened on a line. Strings and chars are not parsed: a bracket inside a literal skews this, which only matters for a
	// declaration with such a literal ahead of its mode argument.
	private static int Unmatched(string line) => line.Count(c => c is ')' or '}' or ']') - line.Count(c => c is '(' or '{' or '[');

	// Walks up from the line holding the mode to the first line of its statement. While the lines walked hold more closers than openers the statement
	// opens above them (an argument list, a block lambda), so a semicolon or brace inside those lines does not end it.
	private static int StatementStart(string[] lines, int i)
	{
		var start = i;
		var open = Unmatched(lines[i]);
		while (start > 0 && (open > 0 || !EndsStatement(lines[start - 1])))
		{
			start--;
			open += Unmatched(lines[start]);
		}
		return start;
	}

	private static bool HasOwnAnnotation(string[] lines, int statementStart)
	{
		for (var j = statementStart - 1; j >= 0 && IsComment(lines[j]); j--)
		{
			if (lines[j].Contains(Marker, StringComparison.Ordinal))
				return true;
		}
		return false;
	}

	// The regex runs over the whole source so that a mode written across lines (errorsAreFatal:<newline>false) is found; the line reported is the match's first.
	private static IEnumerable<int> Unannotated(string source)
	{
		var lines = source.Split('\n');
		foreach (var line in NonDefault().Matches(source).Select(m => source.AsSpan(0, m.Index).Count('\n')))
		{
			if (!IsComment(lines[line]) && !HasOwnAnnotation(lines, StatementStart(lines, line)))
				yield return line + 1;
		}
	}

	[Test]
	public void Guard_FlagsAUseWithoutTheComment() => Unannotated("var l = Loader.For(p, m, Fatal.CheckErrors);").Should().Equal(1);

	[Test]
	public void Guard_AcceptsAUseWithTheCommentAbove() =>
		Unannotated("// TOLERANT LOAD: keeps x\nvar l = Loader.For(p, m, fatal: Fatal.None);").Should().BeEmpty();

	[Test]
	public void Guard_FlagsTheBoolOverloadSelectingTheTolerantMode() =>
		Unannotated("var l = Loader.For(p, m, errorsAreFatal: false);").Should().Equal(1);

	[Test]
	public void Guard_AcceptsTheBoolOverloadWithTheCommentAbove() =>
		Unannotated("// TOLERANT LOAD: keeps x\nvar l = Loader.For(p, m, errorsAreFatal: false);").Should().BeEmpty();

	[Test]
	public void Guard_IgnoresTheBoolOverloadStrictValue() =>
		Unannotated("var l = Loader.For(p, m, errorsAreFatal: true);").Should().BeEmpty();

	[Test]
	public void Guard_OneAnnotationDoesNotCoverTheNextDeclaration() =>
		Unannotated("// TOLERANT LOAD: keeps x\nvar a = Loader.For(p, m, Fatal.None);\nvar b = Loader.For(p, m, Fatal.None);")
			.Should()
			.Equal(3);

	[Test]
	public void Guard_AcceptsAnAnnotationAboveAMultiLineDeclaration() =>
		Unannotated("// TOLERANT LOAD: keeps x\nvar a = Loader.For(\n\tp,\n\tm,\n\tFatal.None);").Should().BeEmpty();

	[Test]
	public void Guard_AcceptsAnAnnotationAboveABlockLambdaDeclaration() =>
		Unannotated(
			"// TOLERANT LOAD: keeps x\nvar a = Loader.For(\n\t(t, f) =>\n\t{\n\t\tvar x = t;\n\t\treturn x;\n\t},\n\tm,\n\tFatal.None);"
		).Should().BeEmpty();

	[Test]
	public void Guard_FlagsABlockLambdaDeclarationWithoutTheComment() =>
		Unannotated("var z = 1;\nvar a = Loader.For(\n\t(t, f) =>\n\t{\n\t\treturn t;\n\t},\n\tm,\n\tFatal.None);").Should().Equal(8);

	[Test]
	public void Guard_FlagsTheBoolOverloadWrappedAfterTheColon() =>
		Unannotated("var l = Loader.For(p, m, errorsAreFatal:\n\tfalse);").Should().Equal(1);

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
