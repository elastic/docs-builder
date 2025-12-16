// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Links;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.InlineParsers;

namespace Elastic.Markdown.Myst.Directives.Button;

/// <summary>
/// Container block for grouping multiple buttons in a row.
/// </summary>
public class ButtonGroupBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "button-group";

	/// <summary>
	/// Horizontal alignment of the button group.
	/// </summary>
	public string Align { get; private set; } = "left";

	public override void FinalizeAndValidate(ParserContext context) =>
		Align = Prop("align") ?? "left";
}

/// <summary>
/// A button directive that renders as a styled link button.
/// </summary>
public class ButtonBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "button";

	private static readonly HashSet<string> ValidTypes = ["primary", "secondary"];
	private static readonly HashSet<string> ValidAligns = ["left", "center", "right"];

	/// <summary>
	/// The button text to display.
	/// </summary>
	public string Text { get; private set; } = string.Empty;

	/// <summary>
	/// The URL the button links to (internal or external).
	/// </summary>
	public string? Link { get; private set; }

	/// <summary>
	/// The resolved link URL (handles relative paths).
	/// </summary>
	public string? ResolvedLink { get; private set; }

	/// <summary>
	/// Button variant: "primary" (filled) or "secondary" (outlined).
	/// </summary>
	public string Type { get; private set; } = "primary";

	/// <summary>
	/// Horizontal alignment for standalone buttons: "left", "center", or "right".
	/// </summary>
	public string Align { get; private set; } = "left";

	/// <summary>
	/// If true, the link opens in a new tab with appropriate security attributes.
	/// </summary>
	public bool External { get; private set; }

	/// <summary>
	/// Whether this button is inside a button group.
	/// </summary>
	public bool IsInGroup { get; private set; }

	/// <summary>
	/// Whether the link is a cross-repository link.
	/// </summary>
	public bool IsCrossLink { get; private set; }

	/// <summary>
	/// Whether the link requires htmx attributes for client-side navigation.
	/// </summary>
	public bool RequiresHtmx => IsCrossLink || (ResolvedLink?.StartsWith('/') == true);

	public override void FinalizeAndValidate(ParserContext context)
	{
		// Get button text from arguments
		Text = Arguments ?? string.Empty;
		if (string.IsNullOrWhiteSpace(Text))
			this.EmitError("Button directive requires text as an argument.");

		// Get and validate link
		Link = Prop("link", "href", "url");
		if (string.IsNullOrWhiteSpace(Link))
		{
			this.EmitError("Button directive requires a :link: property.");
		}
		else
		{
			ResolveLink(context);
		}

		// Get and validate type
		var type = Prop("type", "variant")?.ToLowerInvariant();
		if (type != null && !ValidTypes.Contains(type))
		{
			this.EmitWarning($"Invalid button type '{type}'. Valid types are: primary, secondary. Defaulting to 'primary'.");
			type = "primary";
		}
		Type = type ?? "primary";

		// Get and validate alignment
		var align = Prop("align")?.ToLowerInvariant();
		if (align != null && !ValidAligns.Contains(align))
		{
			this.EmitWarning($"Invalid button alignment '{align}'. Valid alignments are: left, center, right. Defaulting to 'left'.");
			align = "left";
		}
		Align = align ?? "left";

		// Check if inside a button group
		IsInGroup = Parent is ButtonGroupBlock;
	}

	private void ResolveLink(ParserContext context)
	{
		if (string.IsNullOrWhiteSpace(Link))
			return;

		// Get explicit external flag from properties
		var explicitExternal = PropBool("external", "new-tab", "newtab");

		// Check if it's an absolute URL
		if (Uri.TryCreate(Link, UriKind.Absolute, out var uri))
		{
			// Check if it's a cross-link (e.g., kibana://api/index.md)
			if (CrossLinkValidator.IsCrossLink(uri))
			{
				IsCrossLink = true;
				context.Build.Collector.EmitCrossLink(Link);
				if (context.CrossLinkResolver.TryResolve(
						s => this.EmitError(s),
						uri, out var resolvedUri))
				{
					ResolvedLink = resolvedUri.ToString();
				}
				else
				{
					// Fallback to original link if resolution fails (error already emitted)
					ResolvedLink = Link;
				}
				External = explicitExternal;
				return;
			}

			// Regular absolute URL (http/https)
			if (uri.Scheme.StartsWith("http"))
			{
				ResolvedLink = Link;
				// Auto-detect external links (non-elastic.co URLs), unless explicitly set
				External = explicitExternal || !uri.Host.Contains("elastic.co");
				return;
			}
		}

		// Handle relative URLs - these are internal links
		ResolvedLink = DiagnosticLinkInlineParser.UpdateRelativeUrl(context, Link);
		External = explicitExternal;
	}
}

