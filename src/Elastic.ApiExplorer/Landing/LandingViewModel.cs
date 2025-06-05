// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi.Models;

namespace Elastic.ApiExplorer.Landing;

public class LandingViewModel : ApiViewModel
{
	public required ApiLanding Landing { get; init; }
	public required OpenApiInfo ApiInfo { get; init; }
}
