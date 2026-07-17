// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;

namespace Elastic.Documentation.Configuration.Toc.CliReference;

/// <summary>
/// Represents a CLI reference entry in the table of contents, parsed from:
/// <code>
///   - cli: schema/cli.json
///     folder: cli-reference/
///     applies_to:
///       stack: preview
///       serverless: preview
/// </code>
/// </summary>
public record CliReferenceRef(
	string SchemaPath,
	string? SupplementalFolder,
	string? Title,
	string? NavigationTitle,
	string PathRelativeToDocumentationSet,
	string PathRelativeToContainer,
	string Context,
	IReadOnlyCollection<ITableOfContentsItem> Children,
	ApplicableTo? AppliesTo = null
) : ITableOfContentsItem;
