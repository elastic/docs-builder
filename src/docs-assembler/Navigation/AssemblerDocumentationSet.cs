// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Documentation.Assembler.Sourcing;
using Elastic.Markdown;
using Elastic.Markdown.CrossLinks;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Discovery;
using Elastic.Markdown.IO.Navigation;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Navigation;

public record AssemblerDocumentationSet
{
	public AssembleContext AssembleContext { get; }

	public Checkout Checkout { get; }

	public BuildContext BuildContext { get; }

	public DocumentationSet DocumentationSet { get; }

	public AssemblerDocumentationSet(
		ILoggerFactory logger,
		AssembleContext context,
		Checkout checkout,
		CrossLinkResolver crossLinkResolver,
		TableOfContentsTreeCollector treeCollector
	)
	{
		AssembleContext = context;
		Checkout = checkout;

		var env = context.Environment;

		var path = checkout.Directory.FullName;
		var output = env.PathPrefix != null
			? Path.Combine(context.OutputDirectory.FullName, env.PathPrefix)
			: context.OutputDirectory.FullName;

		var gitConfiguration = new GitCheckoutInformation
		{
			RepositoryName = checkout.Repository.Name,
			Ref = checkout.HeadReference,
			Remote = $"elastic/${checkout.Repository.Name}",
			Branch = checkout.Repository.CurrentBranch
		};

		var buildContext = new BuildContext(
			context.Collector,
			context.ReadFileSystem,
			context.WriteFileSystem,
			path,
			output,
			gitConfiguration
		)
		{
			UrlPathPrefix = env.PathPrefix,
			Force = true,
			AllowIndexing = env.AllowIndexing,
			GoogleTagManager = new GoogleTagManagerConfiguration
			{
				Enabled = env.GoogleTagManager.Enabled,
				Id = env.GoogleTagManager.Id,
				Auth = env.GoogleTagManager.Auth,
				Preview = env.GoogleTagManager.Preview,
				CookiesWin = env.GoogleTagManager.CookiesWin
			},
			CanonicalBaseUrl = new Uri("https://www.elastic.co"), // Always use the production URL. In case a page is leaked to a search engine, it should point to the production site.
			SkipMetadata = true,
		};
		BuildContext = buildContext;

		DocumentationSet = new DocumentationSet(buildContext, logger, crossLinkResolver, treeCollector);
	}
}
