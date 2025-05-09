// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.Text;
using Elastic.Markdown.Myst.Settings;

namespace Elastic.Markdown.Slices.Directives;

public class AdmonitionViewModel
{
	public required string Title { get; init; }
	public required string Directive { get; init; }
	public required string? CrossReferenceName { get; init; }
	public required string? Classes { get; init; }
	public required string? Open { get; init; }
}

public class CodeViewModel
{
	public required string? ApiCallHeader { get; init; }
	public required string? Caption { get; init; }
	public required string Language { get; init; }
	public required string? CrossReferenceName { get; init; }
}

public class VersionViewModel
{
	public required string Directive { get; init; }
	public required string VersionClass { get; init; }
	public required string Title { get; init; }
}

public class TabSetViewModel;

public class TabItemViewModel
{
	public required int Index { get; init; }
	public required int TabSetIndex { get; init; }
	public required string Title { get; init; }
	public required string? SyncKey { get; init; }
	public required string? TabSetGroupKey { get; init; }
}
public class IncludeViewModel
{
	public required string Html { get; init; }
}

public class ImageViewModel
{
	public required string? Label { get; init; }
	public required string? Align { get; init; }
	public required string? Alt { get; init; }
	public required string? Title { get; init; }
	public required string? Height { get; init; }
	public required string? Scale { get; init; }
	public required string? Target { get; init; }
	public required string? Width { get; init; }
	public required string? ImageUrl { get; init; }
	public string UniqueImageId { get; } = Guid.NewGuid().ToString("N")[..8];
	public required string? Screenshot { get; init; }

	public string Style
	{
		get
		{
			var sb = new StringBuilder();
			if (Height != null)
				_ = sb.Append($"height: {Height};");
			if (Width != null)
				_ = sb.Append($"width: {Width};");
			return sb.ToString();
		}
	}
}


public class SettingsViewModel
{
	public required YamlSettings SettingsCollection { get; init; }

	public required Func<string, string> RenderMarkdown { get; init; }
}

public class MermaidViewModel;

public class StepperViewModel;

public class StepViewModel
{
	public required string Title { get; init; }
	public required string Anchor { get; init; }
}
