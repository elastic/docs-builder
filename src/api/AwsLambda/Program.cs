// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Api.Core;
using Api.Core.AskAi;
using Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonContext.Default);
});

builder.Services.AddHttpClient();
builder.Services.AddUsecases(Environment.GetEnvironmentVariable("APP_ENVIRONMENT"));

builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi, new SourceGeneratorLambdaJsonSerializer<LambdaJsonSerializerContext>());

var app = builder.Build();

app.MapPost("/ask-ai/stream", async (AskAiRequest askAiRequest, AskAiUsecase askAiUsecase, Cancel ctx) =>
{
	var stream = await askAiUsecase.AskAi(askAiRequest, ctx);
	return Results.Stream(stream, "text/event-stream");
});

app.Run();

[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse), GenerationMode = JsonSourceGenerationMode.Default)]
internal sealed partial class LambdaJsonSerializerContext : JsonSerializerContext { }
