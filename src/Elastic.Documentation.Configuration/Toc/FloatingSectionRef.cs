// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// Mounts a floating section in the table of contents, parsed from <c>- api: elasticsearch</c>.
/// A floating section owns its URL root and its sidebar, and its pages are not Markdown files.
/// <paramref name="Kind"/> is the toc key (<c>api</c>) and <paramref name="Key"/> its value.
/// </summary>
public record FloatingSectionRef(
	string Kind,
	string Key,
	string PathRelativeToDocumentationSet,
	string PathRelativeToContainer,
	string Context,
	IReadOnlyCollection<string>? ExtraKeys = null
) : ITableOfContentsItem
{
	/// <summary>The toc keys that mount a floating section.</summary>
	public static IReadOnlySet<string> Kinds { get; } = new HashSet<string>(StringComparer.Ordinal) { "api" };
}
