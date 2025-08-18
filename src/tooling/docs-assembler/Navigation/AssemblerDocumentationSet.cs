// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Documentation.Assembler.Sourcing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.Links.CrossLinks;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Navigation;

public record AssemblerDocumentationSet
{
	public AssembleContext AssembleContext { get; }

	public Checkout Checkout { get; }

	public BuildContext BuildContext { get; }

	public DocumentationSet DocumentationSet { get; }

	public AssemblerDocumentationSet(
		ILoggerFactory logFactory,
		AssembleContext context,
		Checkout checkout,
		CrossLinkResolver crossLinkResolver,
		TableOfContentsTreeCollector treeCollector,
		IConfigurationContext configurationContext,
		IReadOnlySet<Exporter> availableExporters
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
			Branch = checkout.Repository.GetBranch(env.ContentSource)
		};

		var buildContext = new BuildContext(
			context.Collector,
			context.ReadFileSystem,
			context.WriteFileSystem,
			configurationContext,
			availableExporters,
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
			AssemblerBuild = true
		};
		BuildContext = buildContext;

		DocumentationSet = new DocumentationSet(buildContext, logFactory, context.Collector, crossLinkResolver, treeCollector);
	}
}
