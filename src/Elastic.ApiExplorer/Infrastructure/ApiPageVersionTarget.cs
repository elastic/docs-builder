// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Types;
using Elastic.Documentation.Navigation;

namespace Elastic.ApiExplorer.Infrastructure;

public enum ApiPageVersionTargetKind
{
	Operation,
	Tag,
	Schema
}

public sealed record ApiPageVersionTarget(ApiPageVersionTargetKind Kind, string Identity)
{
	public static ApiPageVersionTarget? FromNavigation(INavigationItem item) =>
		item switch
		{
			OperationNavigationItem operation => new(
				ApiPageVersionTargetKind.Operation,
				ApiUrlBuilder.OperationMoniker(operation.Model.Operation.OperationId, operation.Model.Route)),
			TagNavigationItem tag => new(ApiPageVersionTargetKind.Tag, tag.Index.Model.TagUrlSegment),
			SchemaNavigationItem schema => new(
				ApiPageVersionTargetKind.Schema,
				ApiUrlBuilder.SchemaMoniker(schema.Model.SchemaId)),
			_ => null
		};
}
