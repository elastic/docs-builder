// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.TextField;

public sealed record TextFieldModel
{
	public required string Id { get; init; }
	public required string Name { get; init; }
	public required string Label { get; init; }
	public string Type { get; init; } = "text";
	public string? Placeholder { get; init; }
	public string? Value { get; init; }
	public string? Hint { get; init; }

	/// <summary>When set, the field is marked invalid and the message is announced.</summary>
	public string? Error { get; init; }

	public bool Required { get; init; }
	public bool Disabled { get; init; }

	public string? DescribedBy =>
		string.Join(' ', new[] { Hint is null ? null : $"{Id}-hint", Error is null ? null : $"{Id}-error" }.Where(i => i is not null)) is
		{
			Length: > 0
		} ids ? ids : null;
}
