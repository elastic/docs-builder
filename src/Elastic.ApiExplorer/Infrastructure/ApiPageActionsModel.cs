// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>The Copy page menu: the page's Markdown, plus the whole spec download when the page belongs to an API.</summary>
public sealed record ApiPageActionsModel(string MarkdownUrl, ApiSpecDownload? Spec);
