// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>Multi-language code sample widget with a header language selector.</summary>
public record ApiCodeSampleModel(string IdPrefix, IReadOnlyList<CodeSample> Samples);
