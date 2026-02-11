// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Diagnostics;

public readonly record struct Diagnostic
{
	public Severity Severity { get; init; }
	public int? Line { get; init; }
	public int? Column { get; init; }
	public int? Length { get; init; }
	public string File { get; init; }
	public string Message { get; init; }

	/// <summary>
	/// Optional path to the original source file when File points to a generated/virtual file.
	/// Used to provide context when the virtual file doesn't exist on disk.
	/// </summary>
	public string? OriginalSourceFile { get; init; }
}
