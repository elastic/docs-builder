// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation.Extensions;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.Renderers.LlmMarkdown;
using Markdig.Renderers;
using Microsoft.Extensions.ObjectPool;

namespace Elastic.Markdown.Helpers;

internal static class DocumentationObjectPoolProvider
{
	private static readonly ObjectPoolProvider PoolProvider = new DefaultObjectPoolProvider();

	public static readonly ObjectPool<StringBuilder> StringBuilderPool = PoolProvider.CreateStringBuilderPool(256, 4 * 1024);
	public static readonly ObjectPool<ReusableStringWriter> StringWriterPool = PoolProvider.Create(new ReusableStringWriterPooledObjectPolicy());
	public static readonly ObjectPool<HtmlRenderSubscription> HtmlRendererPool = PoolProvider.Create(new HtmlRendererPooledObjectPolicy());


	private sealed class ReusableStringWriterPooledObjectPolicy : IPooledObjectPolicy<ReusableStringWriter>
	{
		public ReusableStringWriter Create() => new();

		public bool Return(ReusableStringWriter obj)
		{
			obj.Reset();
			return true;
		}
	}

	public sealed class HtmlRenderSubscription
	{
		public required HtmlRenderer HtmlRenderer { get; init; }
		public StringBuilder? RentedStringBuilder { get; internal set; }
	}

	private sealed class HtmlRendererPooledObjectPolicy : IPooledObjectPolicy<HtmlRenderSubscription>
	{
		public HtmlRenderSubscription Create()
		{
			var stringBuilder = StringBuilderPool.Get();
			using var stringWriter = StringWriterPool.Get();
			stringWriter.SetStringBuilder(stringBuilder);
			var renderer = new HtmlRenderer(stringWriter);
			MarkdownParser.Pipeline.Setup(renderer);

			return new HtmlRenderSubscription { HtmlRenderer = renderer, RentedStringBuilder = stringBuilder };
		}

		public bool Return(HtmlRenderSubscription subscription)
		{
			//subscription.RentedStringBuilder = null;
			//return string builder
			if (subscription.RentedStringBuilder is not null)
				StringBuilderPool.Return(subscription.RentedStringBuilder);

			subscription.RentedStringBuilder = null;

			var renderer = subscription.HtmlRenderer;

			//reset string writer
			((ReusableStringWriter)renderer.Writer).Reset();

			// reseed string writer with string builder
			var stringBuilder = StringBuilderPool.Get();
			subscription.RentedStringBuilder = stringBuilder;
			((ReusableStringWriter)renderer.Writer).SetStringBuilder(stringBuilder);
			return true;
		}
	}

}
