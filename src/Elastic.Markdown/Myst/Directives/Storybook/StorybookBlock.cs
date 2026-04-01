// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.Storybook;

public class StorybookBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "storybook";

	public string? StoryRoot { get; private set; }

	public string? StoryId { get; private set; }

	public string? StoryUrl { get; private set; }

	public int Height { get; private set; } = 400;

	public string IframeTitle { get; private set; } = "Storybook story";

	public string? BundleUrl { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		if (!string.IsNullOrWhiteSpace(Arguments))
			this.EmitWarning("storybook directive ignores positional arguments. Use :root: and :id: properties instead.");

		var rawRoot = Prop("root")?.Trim();
		if (string.IsNullOrWhiteSpace(rawRoot))
			rawRoot = Build.Configuration.StorybookRoot;

		if (string.IsNullOrWhiteSpace(rawRoot))
		{
			this.EmitError("storybook directive requires a :root: property or docset.yml storybook.root. Example: :root: /storybook/my-lib");
			return;
		}

		var rawId = Prop("id")?.Trim();
		if (string.IsNullOrWhiteSpace(rawId))
		{
			this.EmitError("storybook directive requires an :id: property. Example: :id: components-button--primary");
			return;
		}

		if (!TryValidateStoryRoot(rawRoot, out var validatedRoot, out var validationError))
		{
			this.EmitError(validationError);
			return;
		}
		if (validatedRoot is null)
		{
			this.EmitError("storybook directive could not resolve a valid :root: value.");
			return;
		}

		StoryRoot = validatedRoot;
		StoryId = rawId;
		StoryUrl = BuildStoryUrl(validatedRoot, rawId);

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

		var rawBundle = Prop("bundle")?.Trim();
		if (string.IsNullOrWhiteSpace(rawBundle))
			rawBundle = Build.Configuration.StorybookBundle;
		if (!string.IsNullOrWhiteSpace(rawBundle))
			BundleUrl = rawBundle;
	}

	private bool TryValidateStoryRoot(string rawRoot, out string? validatedRoot, out string validationError)
	{
		validatedRoot = null;
		validationError = string.Empty;

		if (rawRoot.StartsWith("//", StringComparison.Ordinal))
		{
			validationError = $"storybook directive :root: must be root-relative (starting with a single '/') or match a configured absolute URL in docset.yml under storybook.allowed_roots. Got: {rawRoot}";
			return false;
		}

		Uri rootUri;
		if (rawRoot.StartsWith('/'))
		{
			if (!Uri.TryCreate(new Uri("https://placeholder.elastic.dev"), rawRoot, out rootUri!))
			{
				validationError = $"storybook directive :root: is not a valid root-relative path. Got: {rawRoot}";
				return false;
			}

			var normalizedRoot = NormalizeRoot(rawRoot);
			if (!TryValidateServerRoot(Build.Configuration.StorybookServerRoot, out var validatedServerRoot, out validationError))
				return false;

			validatedRoot = string.IsNullOrWhiteSpace(validatedServerRoot)
				? normalizedRoot
				: CombineServerAndRoot(validatedServerRoot, normalizedRoot);
		}
		else if (Uri.TryCreate(rawRoot, UriKind.Absolute, out rootUri!))
		{
			var normalizedRoot = NormalizeRoot(rawRoot);
			var supportedScheme = rootUri.Scheme is "http" or "https";
			var configuredRoot = Build.Configuration.StorybookAllowedRoots.Contains(normalizedRoot);

			if (supportedScheme && configuredRoot)
				validatedRoot = normalizedRoot;
			else
			{
				validationError = $"storybook directive :root: must be root-relative (starting with a single '/') or match a configured absolute URL in docset.yml under storybook.allowed_roots. Got: {rawRoot}";
				return false;
			}
		}
		else
		{
			validationError = $"storybook directive :root: must be root-relative (starting with a single '/') or match a configured absolute URL in docset.yml under storybook.allowed_roots. Got: {rawRoot}";
			return false;
		}

		if (!string.IsNullOrEmpty(rootUri.Query) || !string.IsNullOrEmpty(rootUri.Fragment))
		{
			validationError = $"storybook directive :root: must not contain query string or fragment content. Got: {rawRoot}";
			return false;
		}

		if (rootUri.AbsolutePath.EndsWith("/iframe.html", StringComparison.OrdinalIgnoreCase)
			|| rootUri.AbsolutePath.Equals("/iframe.html", StringComparison.OrdinalIgnoreCase))
		{
			validationError = $"storybook directive :root: should point to the Storybook root, not iframe.html. Got: {rawRoot}";
			return false;
		}

		return true;
	}

	private static bool TryValidateServerRoot(string? rawServerRoot, out string? validatedServerRoot, out string validationError)
	{
		validatedServerRoot = null;
		validationError = string.Empty;

		if (string.IsNullOrWhiteSpace(rawServerRoot))
			return true;

		if (!Uri.TryCreate(rawServerRoot, UriKind.Absolute, out var serverUri))
		{
			validationError = $"docset.yml storybook.server_root must be an absolute http:// or https:// URL. Got: {rawServerRoot}";
			return false;
		}

		if (serverUri.Scheme is not ("http" or "https"))
		{
			validationError = $"docset.yml storybook.server_root must use http:// or https://. Got: {rawServerRoot}";
			return false;
		}

		if (!string.IsNullOrEmpty(serverUri.Query) || !string.IsNullOrEmpty(serverUri.Fragment))
		{
			validationError = $"docset.yml storybook.server_root must not contain query string or fragment content. Got: {rawServerRoot}";
			return false;
		}

		if (serverUri.AbsolutePath.EndsWith("/iframe.html", StringComparison.OrdinalIgnoreCase)
			|| serverUri.AbsolutePath.Equals("/iframe.html", StringComparison.OrdinalIgnoreCase))
		{
			validationError = $"docset.yml storybook.server_root should point to the Storybook server, not iframe.html. Got: {rawServerRoot}";
			return false;
		}

		validatedServerRoot = NormalizeRoot(rawServerRoot);
		return true;
	}

	private static string NormalizeRoot(string root)
	{
		if (root.Length > 1 && root.EndsWith('/'))
			return root.TrimEnd('/');
		return root;
	}

	private static string CombineServerAndRoot(string serverRoot, string root) =>
		root == "/" ? serverRoot : $"{serverRoot}{root}";

	private static string BuildStoryUrl(string root, string storyId) =>
		$"{(root == "/" ? string.Empty : root.TrimEnd('/'))}/iframe.html?id={Uri.EscapeDataString(storyId)}&viewMode=story";
}
