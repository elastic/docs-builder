// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig;
using Markdig.Renderers;

namespace Elastic.Markdown.Myst.InlineParsers.Substitution;

public static class SubstitutionBuilderExtensions
{
	public static MarkdownPipelineBuilder UseSubstitution(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<SubstitutionMarkdownExtension>();
		return pipeline;
	}
}

public class SubstitutionMarkdownExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		if (!pipeline.InlineParsers.Contains<SubstitutionParser>())
		{
			// Insert the parser before any other parsers
			pipeline.InlineParsers.Insert(0, new SubstitutionParser());
		}
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (!renderer.ObjectRenderers.Contains<SubstitutionRenderer>())
			renderer.ObjectRenderers.Insert(0, new SubstitutionRenderer());
	}
}
