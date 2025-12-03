// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using Documentation.Builder.Arguments;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Elastic.Documentation.Services.ReleaseNotes;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands;

internal sealed class ReleaseNotesCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext
)
{
	/// <summary>
	/// Changelog commands. Use 'changelog add' to create a new changelog fragment.
	/// </summary>
	[Command("")]
	public Task<int> Default()
	{
		collector.EmitError(string.Empty, "Please specify a subcommand. Use 'changelog add' to create a new changelog fragment. Run 'changelog add --help' for usage information.");
		return Task.FromResult(1);
	}

	/// <summary>
	/// Add a new changelog fragment from command-line input
	/// </summary>
	/// <param name="title">Required: A short, user-facing title (max 80 characters)</param>
	/// <param name="type">Required: Type of change (feature, enhancement, bug-fix, breaking-change, etc.)</param>
	/// <param name="products">Required: Products affected in format "product target lifecycle, ..." (e.g., "elasticsearch 9.2.0 ga, cloud-serverless 2025-08-05")</param>
	/// <param name="subtype">Optional: Subtype for breaking changes (api, behavioral, configuration, etc.)</param>
	/// <param name="area">Optional: Area(s) affected (comma-separated or specify multiple times)</param>
	/// <param name="pr">Optional: Pull request URL</param>
	/// <param name="issues">Optional: Issue URL(s) (comma-separated or specify multiple times)</param>
	/// <param name="description">Optional: Additional information about the change (max 600 characters)</param>
	/// <param name="impact">Optional: How the user's environment is affected</param>
	/// <param name="action">Optional: What users must do to mitigate</param>
	/// <param name="featureId">Optional: Feature flag ID</param>
	/// <param name="highlight">Optional: Include in release highlights</param>
	/// <param name="output">Optional: Output directory for the changelog fragment. Defaults to current directory</param>
	/// <param name="ctx"></param>
	[Command("add")]
	public async Task<int> Create(
		string title,
		string type,
		[ProductInfoParser] List<ProductInfo> products,
		string? subtype = null,
		string[]? area = null,
		string? pr = null,
		string[]? issues = null,
		string? description = null,
		string? impact = null,
		string? action = null,
		string? featureId = null,
		bool? highlight = null,
		string? output = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ReleaseNotesService(logFactory, configurationContext);

		var input = new ReleaseNotesInput
		{
			Title = title,
			Type = type,
			Products = products,
			Subtype = subtype,
			Areas = area ?? [],
			Pr = pr,
			Issues = issues ?? [],
			Description = description,
			Impact = impact,
			Action = action,
			FeatureId = featureId,
			Highlight = highlight,
			Output = output
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.CreateReleaseNotes(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}
}
