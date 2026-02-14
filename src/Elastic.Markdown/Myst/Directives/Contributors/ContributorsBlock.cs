// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.InlineParsers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives.Contributors;

/// <summary>Represents a single contributor parsed from the directive body.</summary>
public record Contributor(
	string GitHub,
	string Name,
	string? Title,
	string? Location,
	string AvatarUrl,
	string ProfileUrl
);

/// <summary>
/// A directive that renders a grid of contributor cards with avatars, names, titles, and locations.
/// </summary>
/// <example>
/// :::{contributors}
/// :columns: 4
///
/// - @theletterf
///   name: Fabrizio Ferri-Benedetti
///   title: Senior Software Engineer
///   location: Barcelona, Spain
///   image: ./assets/override.png
///
/// - @costin
///   name: Costin Leau
///   title: Principal Engineer
///   location: Bucharest, Romania
/// :::
/// </example>
public class ContributorsBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "contributors";

	/// <summary>Number of columns in the grid layout.</summary>
	public int Columns { get; private set; } = 4;

	/// <summary>Parsed contributor entries.</summary>
	public IReadOnlyList<Contributor> Contributors => _contributors;

	private readonly List<Contributor> _contributors = [];

	public override void FinalizeAndValidate(ParserContext context)
	{
		if (int.TryParse(Prop("columns"), out var cols) && cols > 0)
			Columns = cols;

		foreach (var child in this)
		{
			if (child is not ListBlock listBlock)
				continue;

			foreach (var item in listBlock)
			{
				if (item is not ListItemBlock listItem)
					continue;

				var contributor = ParseContributor(context, listItem);
				if (contributor is not null)
					_contributors.Add(contributor);
			}
		}

		if (_contributors.Count == 0)
			this.EmitWarning("Contributors directive has no contributor entries.");
	}

	private Contributor? ParseContributor(ParserContext context, ListItemBlock listItem)
	{
		foreach (var block in listItem)
		{
			if (block is not ParagraphBlock paragraph)
				continue;

			var text = paragraph.Lines.ToString();
			if (string.IsNullOrWhiteSpace(text))
				continue;

			var lines = text.Split('\n', StringSplitOptions.TrimEntries);
			if (lines.Length == 0)
				return null;

			var firstLine = lines[0];
			if (!firstLine.StartsWith('@'))
			{
				this.EmitError($"Contributor entry must start with @username, found: '{firstLine}'");
				return null;
			}

			var github = firstLine[1..].Trim();
			if (string.IsNullOrWhiteSpace(github))
			{
				this.EmitError("Contributor entry has an empty @username.");
				return null;
			}

			string? name = null;
			string? title = null;
			string? location = null;
			string? image = null;

			for (var i = 1; i < lines.Length; i++)
			{
				var line = lines[i];
				if (line.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
					name = line[5..].Trim();
				else if (line.StartsWith("title:", StringComparison.OrdinalIgnoreCase))
					title = line[6..].Trim();
				else if (line.StartsWith("location:", StringComparison.OrdinalIgnoreCase))
					location = line[9..].Trim();
				else if (line.StartsWith("image:", StringComparison.OrdinalIgnoreCase))
					image = line[6..].Trim();
			}

			if (string.IsNullOrWhiteSpace(name))
			{
				this.EmitError($"Contributor @{github} is missing a required 'name:' property.");
				return null;
			}

			var avatarUrl = ResolveAvatarUrl(context, github, image);
			var profileUrl = $"https://github.com/{github}";

			return new Contributor(github, name, title, location, avatarUrl, profileUrl);
		}

		return null;
	}

	private static string ResolveAvatarUrl(ParserContext context, string github, string? image)
	{
		if (string.IsNullOrWhiteSpace(image))
			return $"https://github.com/{github}.png?size=200";

		if (Uri.TryCreate(image, UriKind.Absolute, out var uri) && uri.Scheme.StartsWith("http"))
			return image;

		return DiagnosticLinkInlineParser.UpdateRelativeUrl(context, image);
	}
}
