// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;

namespace Elastic.Documentation.Configuration.Assembler;

public record OptimizelyConfiguration
{
	public bool Enabled { get; init; }

	[MemberNotNullWhen(returnValue: true, nameof(Enabled))]
	public string? Id { get; init; }
}
