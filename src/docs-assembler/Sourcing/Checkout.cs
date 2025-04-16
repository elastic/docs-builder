// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Model.Configuration.Assembler;

namespace Documentation.Assembler.Sourcing;

public record Checkout
{
	public required Repository Repository { get; init; }
	public required string HeadReference { get; init; }
	public required IDirectoryInfo Directory { get; init; }
}
