// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.Text.Json.Serialization.Metadata;
using Actions.Core;
using Actions.Core.Services;
using Actions.Core.Summaries;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using FakeItEasy;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Build.Tests;

public static class TestHelpers
{
	public static IConfigurationContext CreateConfigurationContext(
		IFileSystem fileSystem,
		VersionsConfiguration? versionsConfiguration = null,
		ProductsConfiguration? productsConfiguration = null)
	{
		versionsConfiguration ??= new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				{
					VersioningSystemId.Stack, new VersioningSystem
					{
						Id = VersioningSystemId.Stack,
						Current = new SemVersion(8, 0, 0),
						Base = new SemVersion(8, 0, 0)
					}
				}
			},
		};

		if (productsConfiguration is null)
		{
			var products = new Dictionary<string, Product>
			{
				{
					"elasticsearch", new Product
					{
						Id = "elasticsearch",
						DisplayName = "Elasticsearch",
						VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack)
					}
				}
			};
			productsConfiguration = new ProductsConfiguration
			{
				Products = products.ToFrozenDictionary(),
				ProductDisplayNames = products.ToDictionary(p => p.Key, p => p.Value.DisplayName).ToFrozenDictionary()
			};
		}

		var search = new SearchConfiguration { Synonyms = new Dictionary<string, string[]>(), Rules = [], DiminishTerms = [] };
		return new ConfigurationContext
		{
			Endpoints = new DocumentationEndpoints
			{
				Elasticsearch = ElasticsearchEndpoint.Default,
			},
			ConfigurationFileProvider = new ConfigurationFileProvider(new TestLoggerFactory(null), fileSystem),
			VersionsConfiguration = versionsConfiguration,
			ProductsConfiguration = productsConfiguration,
			SearchConfiguration = search,
			LegacyUrlMappings = new LegacyUrlMappingConfiguration { Mappings = [] },
		};
	}
}

/// <summary>
/// A no-op implementation of ICoreService for testing.
/// </summary>
#pragma warning disable IDE0060 // Remove unused parameter - required by interface
public sealed class NullCoreService : ICoreService
{
	public string GetInput(string name, InputOptions? options) => string.Empty;
	public string[] GetMultilineInput(string name, InputOptions? options = null) => [];
	public bool GetBoolInput(string name, InputOptions? options = null) => false;
	public ValueTask SetOutputAsync<T>(string name, T value, JsonTypeInfo<T>? typeInfo = null) => ValueTask.CompletedTask;
	public ValueTask ExportVariableAsync(string name, string value) => ValueTask.CompletedTask;
	public void SetSecret(string secret) { }
	public ValueTask AddPathAsync(string inputPath) => ValueTask.CompletedTask;
	public void SetFailed(string message) { }
	public void SetCommandEcho(bool enabled) { }
	public void WriteDebug(string message) { }
	public void WriteError(string message, AnnotationProperties? properties = null) { }
	public void WriteWarning(string message, AnnotationProperties? properties = null) { }
	public void WriteNotice(string message, AnnotationProperties? properties = null) { }
	public void WriteInfo(string message) { }
	public void StartGroup(string name) { }
	public void EndGroup() { }
	public ValueTask<T> GroupAsync<T>(string name, Func<ValueTask<T>> action) => action();
	public ValueTask SaveStateAsync<T>(string name, T value, JsonTypeInfo<T>? typeInfo = null) => ValueTask.CompletedTask;
	public string GetState(string name) => string.Empty;
	public Summary Summary { get; } = new();
	public bool IsDebug => false;
}
#pragma warning restore IDE0060

public class TestLoggerFactory(ITestOutputHelper? output) : ILoggerFactory
{
	public void AddProvider(ILoggerProvider provider) { }
	public ILogger CreateLogger(string categoryName) => new TestLogger(output);

	public void Dispose() => GC.SuppressFinalize(this);
}

public class TestLogger(ITestOutputHelper? output) : ILogger
{
	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
	public bool IsEnabled(LogLevel logLevel) => true;
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
		output?.WriteLine($"[{logLevel}] {formatter(state, exception)}");
}
