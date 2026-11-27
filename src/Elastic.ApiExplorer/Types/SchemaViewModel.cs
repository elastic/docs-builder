// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Types;

public class SchemaViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiSchema Schema { get; init; }

	/// <summary>Precomputed structural content of the page; built before the slice renders.</summary>
	public required SchemaPageModel Page { get; init; }

	protected override string? LayoutPageTitle => Schema.DisplayName;

	protected override string? LayoutPageDescription => Schema.Schema.Description;
}
