// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Core.Chat;
using Core.Search;
using Core.Suggestions;
using Core.Interfaces;
using Core.Serialization;
using System.Text.Json;
using Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Register HTTP client for ProcessChatUseCase
builder.Services.AddHttpClient<ProcessChatUseCase>();

// Register Infrastructure implementations (External Services)
builder.Services.AddScoped<IGcpTokenGenerator, GcpIdTokenGenerator>();
// builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>(); // TODO: When ready

// Register Use Cases (Core Business Logic)
builder.Services.AddScoped<SearchDocumentsUseCase>();
builder.Services.AddScoped<GetSuggestionsUseCase>();
builder.Services.AddScoped<ProcessChatUseCase>();

var app = builder.Build();

app.UseHttpsRedirection();

// 1. Search Documents Use Case
app.MapPost("/search", async (HttpContext context, SearchDocumentsUseCase useCase) =>
{
	var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
	var searchQuery = JsonSerializer.Deserialize(requestBody, ApiJsonContext.Default.SearchQuery);

	if (searchQuery == null || string.IsNullOrWhiteSpace(searchQuery.Query))
		return Results.BadRequest("Search query is required");

	var result = await useCase.ExecuteAsync(searchQuery, context.RequestAborted);
	return Results.Ok(result);
});

// 2. Get Suggestions Use Case
app.MapPost("/suggestions", async (HttpContext context, GetSuggestionsUseCase useCase) =>
{
	var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
	var suggestionRequest = JsonSerializer.Deserialize(requestBody, ApiJsonContext.Default.SuggestionRequest);

	if (suggestionRequest == null)
		return Results.BadRequest("Invalid suggestion request");

	var result = await useCase.ExecuteAsync(suggestionRequest, context.RequestAborted);
	return Results.Ok(result);
});

// 3. Process Chat Use Case - Full LLM Gateway compatible
app.MapPost("/chat", async (HttpContext context, ProcessChatUseCase useCase) =>
{
	var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
	var chatRequest = JsonSerializer.Deserialize(requestBody, ApiJsonContext.Default.ChatRequest);

	if (chatRequest == null)
		return Results.BadRequest("Invalid chat request format");

	if (!chatRequest.IsValid)
		return Results.BadRequest("Invalid chat request - ThreadId and user question are required");

	context.Response.ContentType = "text/event-stream";
	context.Response.Headers.CacheControl = "no-cache";
	context.Response.Headers.Connection = "keep-alive";

	await useCase.ExecuteAsync(chatRequest, context.Response.Body, context.RequestAborted);

	return Results.Empty;
});

// 4. Process Chat Use Case - Simplified endpoint
app.MapPost("/chat/simple", async (HttpContext context, ProcessChatUseCase useCase) =>
{
	var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
	var simpleRequest = JsonSerializer.Deserialize(requestBody, ApiJsonContext.Default.SimpleChatRequest);

	if (simpleRequest == null || string.IsNullOrWhiteSpace(simpleRequest.Question))
		return Results.BadRequest("Question is required");

	var chatRequest = ChatRequest.CreateFromQuestion(simpleRequest.Question, simpleRequest.ThreadId);

	context.Response.ContentType = "text/event-stream";
	context.Response.Headers.CacheControl = "no-cache";
	context.Response.Headers.Connection = "keep-alive";

	await useCase.ExecuteAsync(chatRequest, context.Response.Body, context.RequestAborted);

	return Results.Empty;
});

app.Run();
