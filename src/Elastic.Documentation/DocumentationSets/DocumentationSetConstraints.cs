// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>
/// Every rule a declared documentation set must satisfy. None yet: today every finding about a documentation set is made while
/// <c>docset.yml</c> and its table of contents are read, or while <c>ConfigurationFile</c> joins it with the catalog, and moves here
/// one rule at a time without changing what is reported.
/// </summary>
[Constraints<DocumentationSetDefinition>]
public static partial class DocumentationSetConstraints;
