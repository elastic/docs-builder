// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Assembler.Sourcing;

/// <summary>Options for cloning assembler repositories, bound from CLI flags via argh <c>[AsParameters]</c>.</summary>
public record AssemblerCloneOptions
{
	public bool? Strict { get; init; }
	public string? Environment { get; init; }
	public bool? FetchLatest { get; init; }
	public bool? AssumeCloned { get; init; }
}
