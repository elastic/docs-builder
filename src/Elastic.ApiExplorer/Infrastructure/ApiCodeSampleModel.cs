// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>Single code card in the examples rail, e.g. a request body.</summary>
public record ApiCodeSampleModel(CodeSample Sample, string? HttpMethod = null, string? Route = null, string? Title = null);
