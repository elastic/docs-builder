// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
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
	/// Release notes commands. Use 'release-notes create' to create a new changelog fragment.
	/// </summary>
	[Command("")]
	public Task<int> Default()
	{
		collector.EmitError(string.Empty, "Please specify a subcommand. Use 'release-notes create' to create a new changelog fragment. Run 'release-notes create --help' for usage information.");
		return Task.FromResult(1);
	}

	/// <summary>
	/// Create a new release notes changelog fragment from command-line input
	/// </summary>
	/// <param name="headline">Required: A short, user-facing headline (max 80 characters)</param>
	/// <param name="type">Required: Type of change (feature, enhancement, bug-fix, breaking-change, etc.)</param>
	/// <param name="product">Required: Product ID(s) affected (comma-separated or specify multiple times)</param>
	/// <param name="subtype">Optional: Subtype for breaking changes (api, behavioral, configuration, etc.)</param>
	/// <param name="area">Optional: Area(s) affected (comma-separated or specify multiple times)</param>
	/// <param name="pr">Optional: Pull request URL</param>
	/// <param name="issues">Optional: Issue URL(s) (comma-separated or specify multiple times)</param>
	/// <param name="description">Optional: Additional information about the change (max 600 characters)</param>
	/// <param name="impact">Optional: How the user's environment is affected</param>
	/// <param name="action">Optional: What users must do to mitigate</param>
	/// <param name="featureId">Optional: Feature flag ID</param>
	/// <param name="highlight">Optional: Include in release highlights</param>
	/// <param name="lifecycle">Optional: Lifecycle stage (preview, beta, ga)</param>
	/// <param name="target">Optional: Target version or date</param>
	/// <param name="id">Optional: Custom ID (auto-generated if not provided)</param>
	/// <param name="output">Optional: Output directory for the changelog fragment. Defaults to current directory</param>
	/// <param name="ctx"></param>
	[Command("create")]
	public async Task<int> Create(
		string headline,
		string type,
		string[] product,
		string? subtype = null,
		string[]? area = null,
		string? pr = null,
		string[]? issues = null,
		string? description = null,
		string? impact = null,
		string? action = null,
		string? featureId = null,
		bool? highlight = null,
		string? lifecycle = null,
		string? target = null,
		int? id = null,
		string? output = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ReleaseNotesService(logFactory, configurationContext);

		var input = new ReleaseNotesInput
		{
			Title = headline,
			Type = type,
			Products = product,
			Subtype = subtype,
			Areas = area ?? [],
			Pr = pr,
			Issues = issues ?? [],
			Description = description,
			Impact = impact,
			Action = action,
			FeatureId = featureId,
			Highlight = highlight,
			Lifecycle = lifecycle,
			Target = target,
			Id = id,
			Output = output
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.CreateReleaseNotes(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}
}
