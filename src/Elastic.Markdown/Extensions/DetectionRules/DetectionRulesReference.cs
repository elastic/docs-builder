// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.IO.Configuration;

namespace Elastic.Markdown.Extensions.DetectionRules;

public record RulesFolderReference(string Path, bool Found, IReadOnlyCollection<ITocItem> Children) : ITocItem;

public record RuleReference(string Path, string SourceDirectory, bool Found, IReadOnlyCollection<ITocItem> Children, DetectionRule Rule)
	: FileReference(Path, Found, false, Children);
