// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Documentation.Configuration.Toc.DetectionRules;

public record DetectionRuleRef(IFileInfo FileInfo, string PathRelativeToDocumentationSet, string Context)
	: FileRef(PathRelativeToDocumentationSet, PathRelativeToDocumentationSet, true, [], Context);
