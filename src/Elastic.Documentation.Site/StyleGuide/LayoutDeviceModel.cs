// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.StyleGuide;

public enum LayoutKind
{
	OneColumn,
	Bands,
	TwoColumns,
	ThreeColumns
}

/// <summary>A window drawn to scale, with the real layout classes inside it.</summary>
public sealed record LayoutDeviceModel(string Title, int WindowWidth, bool Full, LayoutKind Kind)
{
	/// <summary>Both sizes fit side by side in the guide.</summary>
	public string Scale => WindowWidth >= 2000 ? "0.19" : "0.34";

	public int WindowHeight => WindowWidth * 3 / 10;

	/// <summary>The four windows of a layout: contained and full width, in a 1440px and a 2560px window.</summary>
	public static IReadOnlyList<LayoutDeviceModel> Windows(LayoutKind kind) =>
		[
			new("Contained, 1440px window", 1440, false, kind),
			new("Contained, 2560px window", 2560, false, kind),
			new("Full width, 1440px window", 1440, true, kind),
			new("Full width, 2560px window", 2560, true, kind)
		];
}
