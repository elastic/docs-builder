// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Product reference with strongly typed lifecycle.
/// </summary>
public record ProductReference
{
	/// <summary>The product identifier.</summary>
	public required string ProductId { get; init; }

	/// <summary>Optional target version.</summary>
	public string? Target { get; init; }

	/// <summary>The lifecycle stage of the feature for this product.</summary>
	public Lifecycle? Lifecycle { get; init; }
}
