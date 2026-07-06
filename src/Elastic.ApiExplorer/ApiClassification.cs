// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.ApiExplorer;

public record ApiClassification(string Name, string Description, IReadOnlyCollection<ApiTag> Tags) : IApiGroupingModel
{
	/// <inheritdoc />
	public Task RenderAsync(FileSystemStream stream, ApiRenderContext context, CancellationToken ctx = default) => Task.CompletedTask;
}
