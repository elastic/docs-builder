// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>
/// Every rule a declared documentation set must satisfy on its own. None yet: the rules about what a documentation set refers to
/// (cross-links, release notes, API entries, branding, CTAs) are in the stages of <c>DocumentationSetStages</c>, checked as resolution reaches them,
/// and the rest are still made while <c>docset.yml</c> and its table of contents are read, and move here one rule at a time without changing what is reported.
/// </summary>
[Constraints<DocumentationSetDefinition>]
public static partial class DocumentationSetConstraints;
