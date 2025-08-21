// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Markdown.IO.Navigation;

[DebuggerDisplay("CrossLink: {Url}")]
public record CrossLinkNavigationItem : ILeafNavigationItem<INavigationModel>
{
	// Override Url accessor to use ResolvedUrl if available
	string INavigationItem.Url => ResolvedUrl ?? Url;
	public CrossLinkNavigationItem(string url, string? title, DocumentationGroup group, bool hidden = false)
	{
		_url = url;
		NavigationTitle = title ?? GetNavigationTitleFromUrl(url);
		Parent = group;
		NavigationRoot = group.NavigationRoot;
		Hidden = hidden;
	}

	private string GetNavigationTitleFromUrl(string url)
	{
		// Extract a decent title from the URL
		try
		{
			if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
			{
				// Get the last segment of the path and remove extension
				var lastSegment = uri.AbsolutePath.Split('/').Last();
				lastSegment = Path.GetFileNameWithoutExtension(lastSegment);

				// Convert to title case (simple version)
				if (!string.IsNullOrEmpty(lastSegment))
				{
					var words = lastSegment.Replace('-', ' ').Replace('_', ' ').Split(' ');
					var titleCase = string.Join(" ", words.Select(w =>
						string.IsNullOrEmpty(w) ? "" : char.ToUpper(w[0]) + w[1..].ToLowerInvariant()));
					return titleCase;
				}
			}
		}
		catch
		{
			// Fall back to URL if parsing fails
		}

		return url;
	}

	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }
	// Original URL from the cross-link
	private readonly string _url;

	// Store resolved URL for rendering
	public string? ResolvedUrl { get; set; }

	// Implement the INavigationItem.Url property to use ResolvedUrl if available
	public string Url => ResolvedUrl ?? _url; public string NavigationTitle { get; }
	public int NavigationIndex { get; set; }
	public bool Hidden { get; }
	public bool IsCrossLink => true; // This is always a cross-link
	public INavigationModel Model => null!; // Cross-link has no local model
}
