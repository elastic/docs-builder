// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.DocumentationSets;
using Nullean.Entities;

namespace Elastic.Documentation.Codex;

/// <summary>A documentation set together with the codex site whose groups its <c>codex.group</c> names.</summary>
public sealed record DocumentationSetInCodex(DocumentationSetDefinition Docset, CodexConfiguration Codex);

/// <summary>Every rule that relates a documentation set to the codex site it is listed in.</summary>
[Constraints]
public static partial class DocumentationSetInCodexConstraints
{
	// Declared as a relation, so the generator reads which members it relates and the rule is not written twice.
	public static Constraint<DocumentationSetInCodex> CodexGroupKnown { get; } = Relation
		.Reference((DocumentationSetInCodex p) => new[] { p.Docset }, d => d.CodexGroup, p => p.Codex.Groups, g => g.Id)
		.Rule(message: "codex.group names a group the codex site does not define, so the group is created with a title made from its slug.")
		.WithSeverity(Severity.Warning)
		.Describe("D29: A documentation set's codex.group is one of the groups the codex site defines (also G110).");
}
