// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.DocSet;

namespace Elastic.Documentation.Configuration.Plugins.DetectionRules.TableOfContents;

public record RuleReference(
	string RelativePathRelativeToDocumentationSet,
	string SourceDirectory,
	bool Found,
	IReadOnlyCollection<ITableOfContentsItem> Children,
	DetectionRule Rule,
	string Context
)
	: FileRef(RelativePathRelativeToDocumentationSet, RelativePathRelativeToDocumentationSet, true, Children, Context);
