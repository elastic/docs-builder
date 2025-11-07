// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Documentation.Configuration.Plugins.DetectionRules.TableOfContents;

public record RuleReference(IFileInfo FileInfo, string RelativePathRelativeToDocumentationSet, string Context)
	: FileRef(RelativePathRelativeToDocumentationSet, RelativePathRelativeToDocumentationSet, true, [], Context);
