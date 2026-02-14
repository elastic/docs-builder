// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.CodeBlocks;
using Markdig.Parsers;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.Directives.Contributors;

/// <summary>YAML model for a single contributor entry.</summary>
public class ContributorEntry
{
	[YamlMember(Alias = "gh")]
	public string? GitHub { get; set; }

	public string? Name { get; set; }

	public string? Title { get; set; }

	public string? Location { get; set; }

	public string? Image { get; set; }
}

/// <summary>Resolved contributor ready for rendering.</summary>
public record Contributor(
	string? GitHub,
	string Name,
	string? Title,
	string? Location,
	string AvatarUrl,
	string? ProfileUrl
);

/// <summary>
/// A backtick-fenced directive that renders a grid of contributor cards from YAML content.
/// </summary>
/// <example>
/// ```yaml {contributors}
/// - gh: theletterf
///   name: Fabrizio Ferri-Benedetti
///   title: Senior Software Engineer
///   location: Barcelona, Spain
///   image: ./assets/override.png
///
/// - name: Costin Leau
///   title: Principal Engineer
///   location: Bucharest, Romania
/// ```
/// </example>
public class ContributorsBlock(BlockParser parser, ParserContext context)
	: EnhancedCodeBlock(parser, context)
{
	/// <summary>Resolved contributor entries ready for rendering.</summary>
	public IReadOnlyList<Contributor> Contributors => _contributors;

	private readonly List<Contributor> _contributors = [];

	/// <summary>Resolves YAML entries into display-ready contributors.</summary>
	public void ResolveContributors(IReadOnlyList<ContributorEntry> entries, ParserContext parserContext)
	{
		foreach (var entry in entries)
		{
			if (string.IsNullOrWhiteSpace(entry.Name))
			{
				this.EmitError("Contributor entry is missing a required 'name' property.");
				continue;
			}

			var avatarUrl = ResolveAvatarUrl(parserContext, entry.GitHub, entry.Image);
			var profileUrl = !string.IsNullOrWhiteSpace(entry.GitHub)
				? $"https://github.com/{entry.GitHub}"
				: null;

			_contributors.Add(new Contributor(
				entry.GitHub,
				entry.Name,
				entry.Title,
				entry.Location,
				avatarUrl,
				profileUrl
			));
		}

		if (_contributors.Count == 0)
			this.EmitWarning("Contributors directive has no contributor entries.");
	}

	private static string ResolveAvatarUrl(ParserContext context, string? github, string? image)
	{
		if (!string.IsNullOrWhiteSpace(image))
		{
			if (Uri.TryCreate(image, UriKind.Absolute, out var uri) && uri.Scheme.StartsWith("http"))
				return image;

			return InlineParsers.DiagnosticLinkInlineParser.UpdateRelativeUrl(context, image);
		}

		if (!string.IsNullOrWhiteSpace(github))
			return $"https://github.com/{github}.png?size=200";

		return string.Empty;
	}
}
