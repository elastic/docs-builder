// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Infrastructure;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi, new SourceGeneratorLambdaJsonSerializer<LambdaJsonSerializerContext>());
builder.Services.AddElasticDocsApiUsecases(Environment.GetEnvironmentVariable("ENVIRONMENT"));

var app = builder.Build();

var v1 = app.MapGroup("/docs/_api/v1");
v1.MapElasticDocsApiEndpoints();

app.Run();

[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(AskAiRequest))]
internal sealed partial class LambdaJsonSerializerContext : JsonSerializerContext;
