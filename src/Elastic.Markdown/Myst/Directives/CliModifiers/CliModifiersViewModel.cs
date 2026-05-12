// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.CliModifiers;

public class CliModifiersViewModel : DirectiveViewModel
{
	public bool Destructive { get; init; }
	public bool RequiresConfirmation { get; init; }
	public bool RequiresAuth { get; init; }
	public bool Idempotent { get; init; }
	public string? Scope { get; init; }
	public bool Streaming { get; init; }
	public bool LongRunning { get; init; }
}
