// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using SharpYaml;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Diagnostics;

/// <summary>
/// Decides how a specific unknown key is reported. Return <c>null</c> to fall back to the default
/// error. This is the hook for retiring or renaming keys: accept the old key (optionally migrating
/// <see cref="YamlUnmappedMember.Value"/> onto <see cref="YamlUnmappedMember.Instance"/>) and downgrade
/// it to a hint, or give it a tailored message.
/// </summary>
public delegate YamlUnknownKeyOverride? YamlUnknownKeyPolicy(YamlUnmappedMember member);

/// <param name="Severity">How to report the key, or <c>null</c> to not report it at all.</param>
public readonly record struct YamlUnknownKeyOverride(Severity? Severity, string Message)
{
	public static YamlUnknownKeyOverride Suppress => new(null, string.Empty);
}

public static class YamlUnknownKeyPolicies
{
	/// <summary>
	/// Reports the given keys once per document as a hint instead of an error. For keys that exist in
	/// content repositories but that the model does not support; each use should name the issue that
	/// tracks removing the key, after which the key becomes a plain unknown-key error.
	/// </summary>
	public static YamlUnknownKeyPolicy ToleratedAsHint(string where, params string[] keys)
	{
		var reported = new HashSet<string>(StringComparer.Ordinal);
		return member =>
		{
			if (!keys.Contains(member.Name))
				return null;
			return reported.Add(member.Name)
				? new YamlUnknownKeyOverride(Severity.Hint, $"'{member.Name}' is not supported in {where} and is ignored.")
				: YamlUnknownKeyOverride.Suppress;
		};
	}

	/// <summary>
	/// Reports one specific key as a hint instead of an error, scoped to the YAML whose
	/// <see cref="YamlUnmappedMember.SourceName"/> ends with <paramref name="sourceSuffix"/>. For a single
	/// known typo in upstream content that is already being fixed there; <paramref name="message"/> should
	/// name the pull request that fixes it, after which this override is removed.
	/// </summary>
	public static YamlUnknownKeyPolicy ToleratedAsHintForSource(string sourceSuffix, string key, string message)
	{
		var reported = new HashSet<string>(StringComparer.Ordinal);
		return member =>
		{
			var source = member.SourceName?.Replace('\\', '/');
			if (member.Name != key || source is null || !source.EndsWith(sourceSuffix, StringComparison.Ordinal))
				return null;
			return reported.Add(source) ? new YamlUnknownKeyOverride(Severity.Hint, message) : YamlUnknownKeyOverride.Suppress;
		};
	}
}

/// <summary>Where unknown keys found while deserializing one YAML document are reported.</summary>
/// <param name="Write">Receives one diagnostic per unknown key.</param>
/// <param name="File">File the YAML came from; also used as the diagnostic file.</param>
/// <param name="LineOffset">Lines to add when the YAML is embedded in a larger file (e.g. frontmatter).</param>
/// <param name="Policy">Optional per-key override, see <see cref="YamlUnknownKeyPolicy"/>.</param>
public record YamlUnmappedMemberReporting(Action<Diagnostic> Write, string File, int LineOffset = 0, YamlUnknownKeyPolicy? Policy = null)
{
	public YamlUnmappedMemberReporting(
		IDiagnosticsCollector collector,
		string file,
		int lineOffset = 0,
		YamlUnknownKeyPolicy? policy = null
	) : this(collector.Write, file, lineOffset, policy) { }
}

/// <summary>
/// Single place that turns SharpYaml's unmapped-member reporting into diagnostics. Unknown keys are
/// errors: a typo'd key is otherwise silently ignored, and a renamed key silently loses its value.
/// </summary>
public static class YamlUnmappedMemberDiagnostics
{
	private const int MaxListedKeys = 12;

	/// <summary>For deserialization with an <see cref="IDiagnosticsCollector"/> in scope.</summary>
	public static Action<YamlUnmappedMember> CreateCallback(YamlUnmappedMemberReporting reporting) =>
		member =>
		{
			if (ToDiagnostic(member, reporting) is { } diagnostic)
				reporting.Write(diagnostic);
		};

	/// <summary>Shorthand for the common case: no line offset, no policy, file taken from the options' source name.</summary>
	public static Action<YamlUnmappedMember> CreateCallback(IDiagnosticsCollector collector) =>
		member => CreateCallback(new YamlUnmappedMemberReporting(collector, member.SourceName ?? string.Empty))(member);

	/// <summary>
	/// For deserialization with no collector in scope (startup configuration, lambdas): throws a single
	/// <see cref="YamlException"/> listing every unknown key in the document, so one run reveals all of them.
	/// </summary>
	public static Action<IReadOnlyList<YamlUnmappedMember>> CreateThrowingFinalizer() =>
		members =>
		{
			var first = members[0];
			var lines = members.Select(m => $"({m.KeyStart.Line + 1},{m.KeyStart.Column + 1}) {FormatMessage(m)}");
			throw new YamlException(first.SourceName, first.KeyStart, first.ValueStart, string.Join(Environment.NewLine, lines));
		};

	/// <summary>The diagnostic for an unknown key, or <c>null</c> when the policy suppresses it.</summary>
	public static Diagnostic? ToDiagnostic(YamlUnmappedMember member, YamlUnmappedMemberReporting reporting)
	{
		var overridden = reporting.Policy?.Invoke(member);
		if (overridden is { Severity: null })
			return null;
		return new Diagnostic
		{
			Severity = overridden?.Severity ?? Severity.Error,
			File = reporting.File,
			// SharpYaml's Mark.Line/Column are 0-based; +1 to match the 1-based convention used
			// everywhere else in this codebase for user-facing diagnostics.
			Line = member.KeyStart.Line + 1 + reporting.LineOffset,
			Column = member.KeyStart.Column + 1,
			Length = member.Name.Length,
			Message = overridden?.Message ?? FormatMessage(member)
		};
	}

	public static string FormatMessage(YamlUnmappedMember member) =>
		FormatMessage(ParentPath(member), member.Name, member.SuggestedName, member.KnownMemberNames);

	// SharpYaml's Path ends with the unknown key itself ($.toc[1].fiel); the message names the key
	// separately, so only the container it was found in is useful.
	private static string ParentPath(YamlUnmappedMember member)
	{
		var path = member.Path;
		foreach (var suffix in new[] { $".{member.Name}", $"['{member.Name}']" })
		{
			if (path.EndsWith(suffix, StringComparison.Ordinal))
				return path[..^suffix.Length];
		}
		return path;
	}

	/// <summary>
	/// Shared by readers that walk the YAML tree by hand and so never get a <see cref="YamlUnmappedMember"/>.
	/// <paramref name="parentPath"/> is the container the key was found in (<c>$</c> for the document root).
	/// </summary>
	public static string FormatMessage(string parentPath, string name, string? suggestion, IReadOnlyCollection<string> knownNames)
	{
		var hint = suggestion is not null
			? $" Did you mean '{suggestion}'?"
			: knownNames.Count > 0 ? $" Valid keys: {FormatKnownNames(knownNames)}." : "";
		var location = parentPath is "$" or "" ? "" : $" in {parentPath}";
		return $"Unknown key '{name}'{location}.{hint}";
	}

	/// <summary>Closest known key within the same edit-distance budget SharpYaml uses, or <c>null</c>.</summary>
	public static string? Suggest(string name, IEnumerable<string> knownNames)
	{
		var maxDistance = Math.Max(1, name.Length / 3);
		string? best = null;
		var bestDistance = int.MaxValue;
		foreach (var candidate in knownNames)
		{
			if (Math.Abs(candidate.Length - name.Length) > maxDistance)
				continue;
			var distance = EditDistance(name.ToLowerInvariant(), candidate.ToLowerInvariant());
			if (distance <= maxDistance && distance < bestDistance)
				(best, bestDistance) = (candidate, distance);
		}
		return best;
	}

	private static string FormatKnownNames(IReadOnlyCollection<string> knownNames)
	{
		var sorted = knownNames.Order(StringComparer.Ordinal).ToList();
		var listed = string.Join(", ", sorted.Take(MaxListedKeys));
		return sorted.Count > MaxListedKeys ? $"{listed}, …" : listed;
	}

	// Optimal string alignment distance: Levenshtein plus adjacent transpositions.
	private static int EditDistance(string a, string b)
	{
		var d = new int[a.Length + 1, b.Length + 1];
		for (var i = 0; i <= a.Length; i++)
			d[i, 0] = i;
		for (var j = 0; j <= b.Length; j++)
			d[0, j] = j;

		for (var i = 1; i <= a.Length; i++)
		{
			for (var j = 1; j <= b.Length; j++)
			{
				var cost = a[i - 1] == b[j - 1] ? 0 : 1;
				d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
				if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
					d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
			}
		}
		return d[a.Length, b.Length];
	}
}

public static class YamlSerializerOptionsUnmappedExtensions
{
	/// <summary>Reports unknown keys to the collector as diagnostics; replaces any throwing finalizer.</summary>
	public static YamlSerializerOptions WithUnmappedReporting(this YamlSerializerOptions options, YamlUnmappedMemberReporting reporting) =>
		options with
		{
			SourceName = reporting.File,
			UnmappedMemberCallback = YamlUnmappedMemberDiagnostics.CreateCallback(reporting),
			UnmappedMembersFinalizer = null
		};

	/// <summary>Fails deserialization with one exception listing every unknown key; replaces any callback.</summary>
	public static YamlSerializerOptions WithUnmappedKeysThrowing(this YamlSerializerOptions options, string? sourceName = null) =>
		options with
		{
			SourceName = sourceName ?? options.SourceName,
			UnmappedMemberCallback = null,
			UnmappedMembersFinalizer = YamlUnmappedMemberDiagnostics.CreateThrowingFinalizer()
		};
}

/// <summary>For hand-written <see cref="YamlConverter{T}"/>s that read a mapping key by key.</summary>
public static class YamlReaderUnmappedExtensions
{
	/// <summary>
	/// Reports an unknown key through the same options-level callback as generated readers, or skips the
	/// value when nothing is listening. Call with the reader positioned on the key's value.
	/// </summary>
	public static void SkipOrReportUnmapped(this YamlReader reader, Type declaringType, string[] knownNames, string key, object? instance)
	{
		if (reader.IsReportingUnmappedMembers)
			reader.ReadUnmappedMember(reader.BeginUnmappedMember(declaringType, knownNames, key, instance));
		else
			reader.Skip();
	}
}
