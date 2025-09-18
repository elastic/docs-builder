// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.IO;

namespace Elastic.Markdown.Myst.Directives.Anchor;

public class AnchorBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "anchor";

	public string AnchorId { get; private set; } = string.Empty;

	public override void FinalizeAndValidate(ParserContext context)
	{
		// The anchor ID comes from the arguments (the text after {anchor})
		var rawId = Arguments?.Trim();

		if (string.IsNullOrEmpty(rawId))
		{
			context.EmitError("Anchor directive requires an ID argument");
			AnchorId = "invalid-anchor"; // Set a fallback ID to prevent null
			return;
		}

		AnchorId = rawId.Slugify();

		// Set the cross-reference name for linking
		CrossReferenceName = AnchorId;

		// Note: The anchor will be picked up automatically during the document parsing phase
		// through the CrossReferenceName property and the HTML rendering
	}
}
