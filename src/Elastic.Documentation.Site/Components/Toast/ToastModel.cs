// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Toast;

public enum ToastKind
{
	Info,
	Success,
	Warning,
	Danger
}

/// <summary>A short message that confirms or reports something. Script adds the dismiss button.</summary>
public sealed record ToastModel
{
	public required string Message { get; init; }
	public ToastKind Kind { get; init; } = ToastKind.Info;

	public string CssClass => $"toast toast--{Kind.ToString().ToLowerInvariant()}";

	public string Icon => Kind switch
	{
		ToastKind.Success => "checkInCircleFilled",
		ToastKind.Warning => "warningFilled",
		ToastKind.Danger => "errorFilled",
		_ => "help"
	};

	/// <summary>Warnings and errors interrupt; the rest wait their turn.</summary>
	public string Role => Kind is ToastKind.Warning or ToastKind.Danger ? "alert" : "status";
}
