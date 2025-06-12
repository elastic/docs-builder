// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.TableOfContents;
using Elastic.Documentation.Navigation;

namespace Elastic.Documentation.Configuration.Plugins.DetectionRules.TableOfContents;

public record RuleReference(
	ITableOfContentsScope TableOfContentsScope,
	string RelativePath,
	string SourceDirectory,
	bool Found,
	IReadOnlyCollection<ITocItem> Children, DetectionRule Rule
)
	: FileReference(TableOfContentsScope, RelativePath, true, Children);
