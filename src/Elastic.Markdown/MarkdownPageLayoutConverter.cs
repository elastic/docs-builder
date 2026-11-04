// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using SharpYaml;
using SharpYaml.Serialization;

namespace Elastic.Markdown;

/// <summary>
/// Converts <see cref="MarkdownPageLayout"/> using its hyphenated alias names, replacing YamlDotNet's
/// hyphenated-enum naming convention for this enum. SharpYaml's built-in enum converter has no
/// per-member naming hook.
/// </summary>
public class MarkdownPageLayoutConverter : YamlConverter<MarkdownPageLayout>
{
	public override MarkdownPageLayout Read(YamlReader reader)
	{
		var value = reader.GetScalarValue();
		var start = reader.Start;
		var end = reader.End;
		_ = reader.Read();

		return value switch
		{
			"landing-page" => MarkdownPageLayout.LandingPage,
			"not-found" => MarkdownPageLayout.NotFound,
			"archive" => MarkdownPageLayout.Archive,
			"full-search" => MarkdownPageLayout.FullSearch,
			"hub" => MarkdownPageLayout.Hub,
			_ =>
				throw new YamlException(
					reader.SourceName,
					start,
					end,
					$"Invalid layout '{value}'. Expected one of: landing-page, not-found, archive, full-search, hub."
				)
		};
	}

	public override void Write(YamlWriter writer, MarkdownPageLayout value) =>
		writer.WriteScalar(value switch
		{
			MarkdownPageLayout.LandingPage => "landing-page",
			MarkdownPageLayout.NotFound => "not-found",
			MarkdownPageLayout.Archive => "archive",
			MarkdownPageLayout.FullSearch => "full-search",
			MarkdownPageLayout.Hub => "hub",
			_ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
		});
}
