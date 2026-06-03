// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.Json;
using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.Storybook;

public class StorybookBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	private const string SupportedRegistrySchemaVersion = "1";

	private static readonly TimeSpan RegistryFetchTimeout = TimeSpan.FromSeconds(30);

	// Shared across all storybook directives to pool connections; PooledConnectionLifetime bounds DNS staleness in long-lived serve/watch runs.
	private static readonly HttpClient RegistryHttpClient = new(
		new SocketsHttpHandler
		{
			AutomaticDecompression = DecompressionMethods.All,
			PooledConnectionLifetime = TimeSpan.FromMinutes(5)
		}
	)
	{ Timeout = RegistryFetchTimeout };

	public override string Directive => "storybook";

	public string? Project { get; private set; }

	public string? Storybook { get; private set; }

	public string? DocsId { get; private set; }

	public string? StoryId { get; private set; }

	public string? StoryUrl { get; private set; }

	public string? InlineEntry { get; private set; }

	public string? InlineBootstrapJson { get; private set; }

	public int Height { get; private set; } = 400;

	public string IframeTitle { get; private set; } = "Storybook story";

	public bool HasInlineStory => !string.IsNullOrWhiteSpace(InlineEntry);

	public override void FinalizeAndValidate(ParserContext context)
	{
		if (!string.IsNullOrWhiteSpace(Arguments))
			this.EmitWarning("storybook directive ignores positional arguments. Use properties instead.");

		var reference = ResolveReference();
		if (reference is null)
			return;

		if (!TryLoadRegistry(out var registry))
			return;

		var story = FindStory(registry, reference);
		if (story is null)
		{
			this.EmitError($"storybook registry does not contain id '{reference.RawId}'.");
			return;
		}

		if (string.IsNullOrWhiteSpace(story.RenderMode) || !IsSupportedRenderMode(story.RenderMode))
		{
			this.EmitError($"storybook registry id '{reference.RawId}' has unsupported renderMode '{story.RenderMode}'.");
			return;
		}

		if (string.IsNullOrWhiteSpace(story.DocsId) || string.IsNullOrWhiteSpace(story.StorybookId))
		{
			this.EmitError($"storybook registry id '{reference.RawId}' requires docsId and storybookId.");
			return;
		}

		if (story.Iframe is null || string.IsNullOrWhiteSpace(story.Iframe.Url))
		{
			this.EmitError($"storybook registry id '{reference.RawId}' requires iframe.url.");
			return;
		}

		Project = reference.Project;
		Storybook = reference.Storybook ?? story.Alias;
		DocsId = story.DocsId;
		StoryId = story.StorybookId;
		StoryUrl = ResolveRegistryUrl(registry.BaseUrl, story.Iframe.Url);
		Height = story.Height ?? Height;

		if (story.RenderMode.Equals("inline", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(story.Inline?.Entry))
		{
			InlineEntry = ResolveRegistryUrl(registry.BaseUrl, story.Inline.Entry);
			if (story.Inline.Bootstrap is not null)
				InlineBootstrapJson = SerializeBootstrap(registry.BaseUrl, story.Inline.Bootstrap);
		}

		var rawHeight = Prop("height");
		if (!string.IsNullOrWhiteSpace(rawHeight))
		{
			if (int.TryParse(rawHeight.Trim(), out var parsedHeight) && parsedHeight > 0)
				Height = parsedHeight;
			else
				this.EmitWarning($"storybook directive :height: must be a positive integer. Got '{rawHeight}', using default {Height}px.");
		}

		var rawTitle = Prop("title");
		if (!string.IsNullOrWhiteSpace(rawTitle))
			IframeTitle = rawTitle.Trim();
	}

	private StoryReference? ResolveReference()
	{
		var rawId = Prop("id")?.Trim();
		var project = Prop("project")?.Trim();
		var storybook = Prop("storybook")?.Trim();
		var component = Prop("component")?.Trim();
		var story = Prop("story")?.Trim();

		if (!string.IsNullOrWhiteSpace(rawId))
			return StoryReference.FromId(rawId, project, storybook, component, story);

		if (string.IsNullOrWhiteSpace(project))
		{
			this.EmitError("storybook directive requires :id: or :project:.");
			return null;
		}

		if (string.IsNullOrWhiteSpace(storybook))
		{
			this.EmitError("storybook directive requires :id: or :storybook:.");
			return null;
		}

		if (string.IsNullOrWhiteSpace(story))
		{
			this.EmitError("storybook directive requires :id: or :story:.");
			return null;
		}

		var docsId = string.IsNullOrWhiteSpace(component) ? story : $"{component}--{story}";
		return new StoryReference(project, storybook, docsId, component, story, $"{project}:{storybook}:{docsId}");
	}

	private bool TryLoadRegistry(out StorybookRegistry registry)
	{
		registry = new StorybookRegistry();

		var rawRegistry = Build.Configuration.StorybookRegistry;
		if (string.IsNullOrWhiteSpace(rawRegistry))
		{
			this.EmitError("storybook directive requires docset.yml storybook.registry.");
			return false;
		}

		try
		{
			var registryJson = ReadRegistry(rawRegistry);
			return TryDeserializeRegistry(rawRegistry, registryJson, out registry);
		}
		catch (Exception e)
		{
			this.EmitError($"storybook registry could not be read: {rawRegistry}", e);
			return false;
		}
	}

	private string ReadRegistry(string rawRegistry)
	{
		if (Uri.TryCreate(rawRegistry, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
		{
			// FinalizeAndValidate is synchronous across all directives, so the fetch is sync-over-async here.
			// Bound it with an explicit timeout so an unresponsive registry host can never stall the build.
			using var cts = new CancellationTokenSource(RegistryFetchTimeout);
			return RegistryHttpClient.GetStringAsync(uri, cts.Token).GetAwaiter().GetResult();
		}

		var registryPath = Path.IsPathRooted(rawRegistry)
			? rawRegistry
			: Build.ReadFileSystem.Path.Combine(Build.DocumentationSourceDirectory.FullName, rawRegistry);
		return Build.ReadFileSystem.File.ReadAllText(registryPath);
	}

	private bool TryDeserializeRegistry(string rawRegistryPath, string registryJson, out StorybookRegistry registry)
	{
		registry = new StorybookRegistry();
		try
		{
			registry = JsonSerializer.Deserialize(registryJson, StorybookRegistryJsonContext.Default.StorybookRegistry)!;
		}
		catch (JsonException e)
		{
			this.EmitError($"storybook registry could not be parsed: {rawRegistryPath}", e);
			return false;
		}

		if (registry is null || registry.Stories.Count == 0)
		{
			this.EmitError($"storybook registry is empty: {rawRegistryPath}");
			return false;
		}

		var schemaVersion = RegistrySchemaVersion(registry.SchemaVersion);
		if (!schemaVersion.Equals(SupportedRegistrySchemaVersion, StringComparison.Ordinal))
		{
			this.EmitError($"storybook registry schemaVersion '{schemaVersion}' is not supported. Expected '{SupportedRegistrySchemaVersion}'.");
			return false;
		}

		return true;
	}

	private static string RegistrySchemaVersion(JsonElement schemaVersion) =>
		schemaVersion.ValueKind switch
		{
			JsonValueKind.Number => schemaVersion.GetRawText(),
			JsonValueKind.String => schemaVersion.GetString() ?? string.Empty,
			_ => string.Empty
		};

	private static StorybookRegistryStory? FindStory(StorybookRegistry registry, StoryReference reference)
	{
		if (registry.Stories.TryGetValue(reference.RawId, out var rawMatch))
			return rawMatch;

		var namespacedId = $"{reference.Project}:{reference.Storybook}:{reference.DocsId}";
		if (registry.Stories.TryGetValue(namespacedId, out var namespacedMatch))
			return namespacedMatch;

		var matches = registry.Stories
			.Where(story => MatchesReferenceScope(story.Key, story.Value, reference))
			.Select(story => story.Value)
			.Where(story =>
				story.DocsId?.Equals(reference.DocsId, StringComparison.OrdinalIgnoreCase) == true
				|| story.StorybookId?.Equals(reference.DocsId, StringComparison.OrdinalIgnoreCase) == true)
			.ToArray();

		return matches.Length == 1 ? matches[0] : null;
	}

	private static bool MatchesReferenceScope(string registryId, StorybookRegistryStory story, StoryReference reference)
	{
		var parts = registryId.Split(':', 3, StringSplitOptions.TrimEntries);
		if (!string.IsNullOrWhiteSpace(reference.Project) && (parts.Length != 3 || !parts[0].Equals(reference.Project, StringComparison.OrdinalIgnoreCase)))
			return false;

		if (string.IsNullOrWhiteSpace(reference.Storybook))
			return true;

		var registryStorybook = parts.Length == 3 ? parts[1] : story.Alias;
		return registryStorybook?.Equals(reference.Storybook, StringComparison.OrdinalIgnoreCase) == true
			|| story.Alias?.Equals(reference.Storybook, StringComparison.OrdinalIgnoreCase) == true;
	}

	private static bool IsSupportedRenderMode(string renderMode) =>
		renderMode.Equals("inline", StringComparison.OrdinalIgnoreCase) || renderMode.Equals("iframe", StringComparison.OrdinalIgnoreCase);

	private static string SerializeBootstrap(string? baseUrl, StorybookRegistryBootstrap bootstrap)
	{
		var resolvedBootstrap = new StorybookRegistryBootstrap
		{
			PublicPath = ResolveRegistryUrl(baseUrl, bootstrap.PublicPath),
			Scripts = bootstrap.Scripts.Select(script => ResolveRegistryUrl(baseUrl, script)!).ToArray(),
			Styles = bootstrap.Styles.Select(style => ResolveRegistryUrl(baseUrl, style)!).ToArray()
		};
		return JsonSerializer.Serialize(resolvedBootstrap, StorybookRegistryJsonContext.Default.StorybookRegistryBootstrap);
	}

	private static string? ResolveRegistryUrl(string? baseUrl, string? rawUrl)
	{
		if (string.IsNullOrWhiteSpace(rawUrl))
			return rawUrl;

		var trimmed = rawUrl.Trim();
		if (Uri.TryCreate(trimmed, UriKind.Absolute, out _))
			return trimmed;

		if (!string.IsNullOrWhiteSpace(baseUrl) && Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
			return new Uri(baseUri, trimmed).ToString();

		return trimmed;
	}

	private sealed record StoryReference(string? Project, string? Storybook, string DocsId, string? Component, string? StoryName, string RawId)
	{
		public static StoryReference FromId(string rawId, string? project, string? storybook, string? component, string? story)
		{
			var parts = rawId.Split(':', 3, StringSplitOptions.TrimEntries);
			if (parts.Length == 3)
				return new StoryReference(parts[0], parts[1], parts[2], component, story, rawId);

			return new StoryReference(project, storybook, rawId, component, story, rawId);
		}
	}
}
