// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;

namespace Elastic.Documentation.Mcp.Remote.Tools;

/// <summary>Provides content type templates and guidelines with GitHub-first fetching and embedded fallback.</summary>
public class ContentTypeProvider(HttpClient httpClient)
{
	private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(3);

	private static readonly Dictionary<string, string> TemplateUrls = new()
	{
		["overview"] = "https://raw.githubusercontent.com/elastic/docs-content/main/contribute-docs/content-types/_snippets/templates/overview-template.md",
		["how-to"] = "https://raw.githubusercontent.com/elastic/docs-content/main/contribute-docs/content-types/_snippets/templates/how-to-template.md",
		["tutorial"] = "https://raw.githubusercontent.com/elastic/docs-content/main/contribute-docs/content-types/_snippets/templates/tutorial-template.md",
		["troubleshooting"] = "https://raw.githubusercontent.com/elastic/docs-content/main/contribute-docs/content-types/_snippets/templates/troubleshooting-template.md"
	};

	private static readonly Dictionary<string, string> EmbeddedTemplateNames = new()
	{
		["overview"] = "Elastic.Documentation.Mcp.Remote.Resources.Templates.overview.md.txt",
		["how-to"] = "Elastic.Documentation.Mcp.Remote.Resources.Templates.how-to.md.txt",
		["tutorial"] = "Elastic.Documentation.Mcp.Remote.Resources.Templates.tutorial.md.txt",
		["troubleshooting"] = "Elastic.Documentation.Mcp.Remote.Resources.Templates.troubleshooting.md.txt",
		["changelog"] = "Elastic.Documentation.Mcp.Remote.Resources.Templates.changelog.yaml"
	};

	private static readonly Dictionary<string, string> EmbeddedGuidelineNames = new()
	{
		["overview"] = "Elastic.Documentation.Mcp.Remote.Resources.Guidelines.overview.md.txt",
		["how-to"] = "Elastic.Documentation.Mcp.Remote.Resources.Guidelines.how-to.md.txt",
		["tutorial"] = "Elastic.Documentation.Mcp.Remote.Resources.Guidelines.tutorial.md.txt",
		["troubleshooting"] = "Elastic.Documentation.Mcp.Remote.Resources.Guidelines.troubleshooting.md.txt",
		["changelog"] = "Elastic.Documentation.Mcp.Remote.Resources.Guidelines.changelog.md.txt"
	};

	public static readonly string[] ValidContentTypes = ["overview", "how-to", "tutorial", "troubleshooting", "changelog"];

	public static bool IsValidContentType(string contentType) =>
		EmbeddedTemplateNames.ContainsKey(contentType);

	/// <summary>Gets a template for the given content type. Tries GitHub first, falls back to embedded.</summary>
	public async Task<(string Template, string Source)> GetTemplateAsync(string contentType, CancellationToken cancellationToken = default)
	{
		// Try GitHub first for non-changelog types (changelog has no upstream template file)
		if (TemplateUrls.TryGetValue(contentType, out var url))
		{
			var fetched = await FetchFromGitHubAsync(url, cancellationToken);
			if (fetched is not null)
				return (fetched, "github");
		}

		return (ReadEmbeddedResource(EmbeddedTemplateNames[contentType]), "embedded");
	}

	/// <summary>Gets guidelines for the given content type. Always reads from embedded resources.</summary>
	public string GetGuidelines(string contentType) =>
		ReadEmbeddedResource(EmbeddedGuidelineNames[contentType]);

	private async Task<string?> FetchFromGitHubAsync(string url, CancellationToken cancellationToken)
	{
		try
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(FetchTimeout);

			var response = await httpClient.GetAsync(url, cts.Token);
			if (response.IsSuccessStatusCode)
				return await response.Content.ReadAsStringAsync(cts.Token);
		}
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			// Timeout — fall through to embedded fallback
		}
		catch (HttpRequestException)
		{
			// Network error — fall through to embedded fallback
		}

		return null;
	}

	private static string ReadEmbeddedResource(string resourceName)
	{
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream(resourceName)
			?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}
