// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>Model for the API Explorer code block partial (Myst-style highlight wrappers and copy support).</summary>
public record ApiCodeBlockModel(string HighlightClass, string Source);
