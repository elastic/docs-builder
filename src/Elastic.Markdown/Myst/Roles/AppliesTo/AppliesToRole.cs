// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.FrontMatter;
using Markdig;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;

namespace Elastic.Markdown.Myst.Roles.AppliesTo;

[DebuggerDisplay("{GetType().Name} Line: {Line}, Role: {Role}, Content: {Content}")]
public class AppliesToRole : RoleLeaf, IApplicableToElement
{
	public AppliesToRole(string role, string content, InlineProcessor parserContext) : base(role, content)
	{
		BuildContext = parserContext.GetContext().Build;
		AppliesTo = ParseApplicableTo(content, parserContext);
	}

	public ApplicableTo? AppliesTo { get; }

	public BuildContext BuildContext { get; }

	private ApplicableTo? ParseApplicableTo(string yaml, InlineProcessor processor)
	{
		try
		{
			var applicableTo = YamlSerialization.Deserialize<ApplicableTo>(yaml, BuildContext.VersionsConfiguration, [.. BuildContext.VersionsConfiguration.Products.Keys]);
			if (applicableTo.Diagnostics is null)
				return applicableTo;
			foreach (var (severity, message) in applicableTo.Diagnostics)
				processor.Emit(severity, this, Role.Length + yaml.Length, message);
			applicableTo.Diagnostics = null;
			return applicableTo;
		}
		catch (Exception e)
		{
			processor.EmitError(this, Role.Length + yaml.Length, $"Unable to parse applies_to role: {{{Role}}}{yaml}", e);
		}

		return null;
	}
}

public class AppliesToRoleParser : RoleParser<AppliesToRole>
{
	protected override AppliesToRole CreateRole(string role, string content, InlineProcessor parserContext) =>
		new(role, content, parserContext);

	protected override bool Matches(ReadOnlySpan<char> role) => role is "{applies_to}";
}
public class PreviewRoleParser : RoleParser<AppliesToRole>
{
	protected override AppliesToRole CreateRole(string role, string content, InlineProcessor parserContext)
	{
		content = SemVersion.TryParse(content, out _)
			? $"product: preview {content}"
			: SemVersion.TryParse(content + ".0", out var version)
				? $"product: preview {version}"
				: "product: preview";
		return new AppliesToRole(role, content, parserContext);
	}

	protected override bool Matches(ReadOnlySpan<char> role) => role is "{preview}";
}

public static class InlineAppliesToExtensions
{
	public static MarkdownPipelineBuilder UseInlineAppliesTo(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<InlineAppliesToExtension>();
		return pipeline;
	}
}

public class InlineAppliesToExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		_ = pipeline.InlineParsers.InsertBefore<CodeInlineParser>(new AppliesToRoleParser());
		_ = pipeline.InlineParsers.InsertAfter<AppliesToRoleParser>(new PreviewRoleParser());
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) =>
		renderer.ObjectRenderers.InsertBefore<CodeInlineRenderer>(new AppliesToRoleHtmlRenderer());
}
