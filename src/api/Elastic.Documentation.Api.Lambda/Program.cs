// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Elastic.Documentation.Api.Infrastructure;
using Elastic.Documentation.ServiceDefaults;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddAppLogging(LogLevel.Information);

builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi, new SourceGeneratorLambdaJsonSerializer<LambdaJsonSerializerContext>());
builder.Services.AddElasticDocsApiUsecases(Environment.GetEnvironmentVariable("APP_ENVIRONMENT"));
builder.WebHost.UseKestrelHttpsConfiguration();

var app = builder.Build();

var v1 = app.MapGroup("/v1");
v1.MapElasticDocsApiEndpoints();

app.Run();

[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse), GenerationMode = JsonSourceGenerationMode.Default)]
internal sealed partial class LambdaJsonSerializerContext : JsonSerializerContext;
