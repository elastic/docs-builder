// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using Elastic.Documentation.AppliesTo;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;

namespace Elastic.Markdown.Myst.Directives.AppliesSwitch;

public class AppliesSwitchBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "applies-switch";

	public int Index { get; set; }
	public string GetGroupKey() => Prop("group") ?? "applies-switches";

	public override void FinalizeAndValidate(ParserContext context) => Index = FindIndex();

	private int _index = -1;

	// For simplicity, we use the line number as the index.
	// It's not ideal, but it's unique.
	// This is more efficient than finding the root block and then finding the index.
	public int FindIndex()
	{
		if (_index > -1)
			return _index;

		_index = Line;
		return _index;
	}
}

public class AppliesItemBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context), IBlockTitle
{
	public override string Directive => "applies-item";

	public string AppliesToDefinition { get; private set; } = default!;
	public string Title => AppliesToDefinition; // IBlockTitle implementation
	public int Index { get; private set; }
	public int AppliesSwitchIndex { get; private set; }
	public string? AppliesSwitchGroupKey { get; private set; }
	public string? SyncKey { get; private set; }
	public bool Selected { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		if (string.IsNullOrWhiteSpace(Arguments))
			this.EmitError("{applies-item} requires an argument with applies_to definition.");

		AppliesToDefinition = (Arguments ?? "{undefined}").ReplaceSubstitutions(context);
		Index = Parent!.IndexOf(this);

		var appliesSwitch = Parent as AppliesSwitchBlock;

		AppliesSwitchIndex = appliesSwitch?.FindIndex() ?? -1;
		AppliesSwitchGroupKey = appliesSwitch?.GetGroupKey();

		// Auto-generate sync key from applies_to definition if not provided
		SyncKey = Prop("sync") ?? GenerateSyncKey(AppliesToDefinition);
		Selected = PropBool("selected");
	}

	public static string GenerateSyncKey(string appliesToDefinition)
	{
		// Parse the YAML to get the ApplicableTo object, then use its hash
		// This ensures both simple syntax and YAML objects produce consistent sync keys
		try
		{
			var applicableTo = YamlSerialization.Deserialize<ApplicableTo>(appliesToDefinition);
			if (applicableTo != null)
			{
				// Use the object's hash for a consistent, unique identifier
				return $"applies-{Math.Abs(applicableTo.GetHashCode())}";
			}
		}
		catch
		{
			// If parsing fails, fall back to the original definition
		}

		// Fallback to original definition if parsing fails
		return appliesToDefinition.Slugify().Replace(".", "-");
	}
}
