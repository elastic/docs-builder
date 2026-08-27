// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer.Supplemental;
using Elastic.Documentation;
using Elastic.Documentation.Diagnostics;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests.Supplemental;

public class ApiSupplementalValidationTests(ApiExplorerFixture fixture) : IClassFixture<ApiExplorerFixture>
{
	private const string Folder = "/docs/api/fixture";

	[Fact]
	public void Validate_UnmatchedOperationFileOnLatest_EmitsErrorNamingFile()
	{
		var collector = Validate(FolderWith(("op-does-not-exist.md", "# supplemental")), fixture.Document, "main");

		collector
			.ErrorMessages
			.Should()
			.ContainSingle(m => m.Contains("op-does-not-exist.md") && m.Contains("does not match any operationId in the latest spec"));
	}

	[Fact]
	public void Validate_UnmatchedTagFileOnLatest_EmitsError()
	{
		var collector = Validate(FolderWith(("tag-does-not-exist.md", "# supplemental")), fixture.Document, "main");

		collector
			.ErrorMessages
			.Should()
			.ContainSingle(m => m.Contains("tag-does-not-exist.md") && m.Contains("does not match any tag in the latest spec"));
	}

	[Fact]
	public void Validate_IgnoredFileOnLatest_EmitsNoError()
	{
		var collector = Validate(FolderWith(("random-notes.md", "# notes")), fixture.Document, "main");

		collector.Errors.Should().Be(0);
	}

	[Fact]
	public void Validate_KnownOperationWithNoUnknownKeys_EmitsNoError()
	{
		var collector = Validate(FolderWith(("op-search.md", "Returns hits that match the query.")), fixture.Document, "main");

		collector.Errors.Should().Be(0);
	}

	[Fact]
	public void Validate_UnknownParameterOnLatest_EmitsErrorNamingOperationAndParameter()
	{
		var collector = Validate(
			FolderWith(("op-search.md", """
			## Parameters

			: `nope`
			  Not a search parameter.
			""")),
			fixture.Document,
			"main"
		);

		collector
			.ErrorMessages
			.Should()
			.ContainSingle(m => m.Contains("Parameter 'nope'") && m.Contains("operation 'search'") && m.Contains("the latest spec"));
	}

	[Fact]
	public void Validate_UnknownRequestBodyFieldOnLatest_EmitsError()
	{
		var collector = Validate(
			FolderWith(
				("op-search.md", """
			## Request body

			: `query`
			  Known field.

			: `fields`
			  Also a known field.

			: `nope_field`
			  Not a request body field.
			""")
			),
			fixture.Document,
			"main"
		);

		collector
			.ErrorMessages
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Contain("Request body field 'nope_field'")
			.And
			.Contain("operation 'search'")
			.And
			.Contain("the latest spec");
	}

	[Fact]
	public void Validate_NestedRequestBodyField_EmitsNoError()
	{
		var collector = Validate(
			FolderWith(
				("op-search.md", """
			## Request body

			: `bool`
			  Nested under query; the renderer matches by leaf name.
			""")
			),
			SpecWithNestedRequestBody("search", "query", "bool"),
			"main"
		);

		collector.Errors.Should().Be(0);
	}

	[Fact]
	public void Validate_ListedRealParameter_EmitsNoError()
	{
		var collector = Validate(
			FolderWith(
				("op-search.md", """
			## Parameters

			: `q`
			  A query in the Lucene query string syntax.
			""")
			),
			fixture.Document,
			"main"
		);

		collector.Errors.Should().Be(0);
	}

	[Fact]
	public void Validate_UnmatchedBaseFileOnOlderVersion_EmitsNoError()
	{
		var collector = Validate(FolderWith(("op-search.md", "# supplemental")), SpecWith("ping"), "8");

		collector.Errors.Should().Be(0);
	}

	[Fact]
	public void Validate_UnmatchedBaseFileWhenLatestIsNumeric_EmitsError()
	{
		var collector = Validate(
			FolderWith(("op-does-not-exist.md", "# supplemental")),
			SpecWith("ping"),
			"8",
			emitUnmatchedBaseFiles: true
		);

		collector
			.ErrorMessages
			.Should()
			.ContainSingle(m => m.Contains("op-does-not-exist.md") && m.Contains("does not match any operationId in the latest spec"));
	}

	[Fact]
	public void Validate_UnmatchedBaseFileWhenLatestMonikerIsNonNumeric_EmitsError()
	{
		var collector = Validate(
			FolderWith(("op-does-not-exist.md", "# supplemental")),
			SpecWith("ping"),
			"next",
			emitUnmatchedBaseFiles: true
		);

		collector
			.ErrorMessages
			.Should()
			.ContainSingle(m => m.Contains("op-does-not-exist.md") && m.Contains("does not match any operationId in the latest spec"));
	}

	[Fact]
	public void Validate_UnknownParameterOnNonNumericLatest_EmitsError()
	{
		var collector = Validate(
			FolderWith(("op-search.md", """
			## Parameters

			: `nope`
			  Not a search parameter.
			""")),
			fixture.Document,
			"next"
		);

		collector
			.ErrorMessages
			.Should()
			.ContainSingle(m => m.Contains("Parameter 'nope'") && m.Contains("operation 'search'") && m.Contains("the latest spec"));
	}

	[Fact]
	public void Validate_UnknownParameterOnOlderVersionMatchedBaseFile_EmitsError()
	{
		var collector = Validate(
			FolderWith(("op-search.md", """
			## Parameters

			: `pretty`
			  Removed in this version.
			""")),
			SpecWith("search", "q"),
			"8"
		);

		collector
			.ErrorMessages
			.Should()
			.ContainSingle(m => m.Contains("Parameter 'pretty'") && m.Contains("operation 'search'") && m.Contains("version 8"));
	}

	[Fact]
	public void Validate_VersionSuffixedUnknownOperation_EmitsErrorNamingVersion()
	{
		var collector = Validate(FolderWith(("op-nope.v8.md", "# supplemental")), SpecWith("ping"), "8");

		collector
			.ErrorMessages
			.Should()
			.ContainSingle(m => m.Contains("op-nope.v8.md") && m.Contains("does not match any operationId in version 8"));
	}

	[Fact]
	public void Validate_VersionSuffixedMatchingOperation_EmitsNoUnmatchedError()
	{
		var collector = Validate(FolderWith(("op-search.v8.md", "Returns hits that match the query.")), fixture.Document, "8");

		collector.Errors.Should().Be(0);
	}

	[Fact]
	public void Validate_VersionSuffixedUnknownTag_EmitsErrorNamingVersion()
	{
		var collector = Validate(FolderWith(("tag-nope.v8.md", "# supplemental")), SpecWith("ping"), "8");

		collector
			.ErrorMessages
			.Should()
			.ContainSingle(m => m.Contains("tag-nope.v8.md") && m.Contains("does not match any tag in version 8"));
	}

	[Fact]
	public void Validate_VersionSuffixedTagSlugCollision_EmitsError()
	{
		var spec = SpecWith("ping");
		spec.Tags = new HashSet<OpenApiTag> { new() { Name = "foo bar" }, new() { Name = "foo-bar" } };

		var collector = Validate(FolderWith(("tag-foo-bar.v8.md", "# supplemental")), spec, "8");

		collector
			.ErrorMessages
			.Should()
			.ContainSingle(m => m.Contains("tag-foo-bar.v8.md") && m.Contains("does not match any tag in version 8"));
	}

	[Fact]
	public void Validate_VersionSuffixedUnknownParameter_EmitsError()
	{
		var collector = Validate(
			FolderWith(
				("op-search.v8.md", """
			## Parameters

			: `nope`
			  Not a search parameter.
			""")
			),
			fixture.Document,
			"8"
		);

		collector
			.ErrorMessages
			.Should()
			.ContainSingle(m => m.Contains("Parameter 'nope'") && m.Contains("operation 'search'") && m.Contains("version 8"));
	}

	private static CapturingDiagnosticsCollector Validate(IDirectoryInfo folder, OpenApiDocument document, string moniker) =>
		Validate(folder, document, moniker, emitUnmatchedBaseFiles: moniker == "main");

	private static CapturingDiagnosticsCollector Validate(
		IDirectoryInfo folder,
		OpenApiDocument document,
		string moniker,
		bool emitUnmatchedBaseFiles
	)
	{
		var discovery = ApiSupplementalDiscovery.Discover(folder, document);
		var collector = new CapturingDiagnosticsCollector();
		ApiSupplementalValidator.Validate(discovery, new(document, collector, moniker, EmitUnmatchedBaseFiles: emitUnmatchedBaseFiles));
		return collector;
	}

	private static IDirectoryInfo FolderWith(params (string Name, string Body)[] files)
	{
		var data = files.ToDictionary(f => $"{Folder}/{f.Name}", f => new MockFileData(f.Body));
		return new MockFileSystem(data).DirectoryInfo.New(Folder);
	}

	private static OpenApiDocument SpecWith(string operationId, params string[] parameterNames) =>
		new()
		{
			Info = new OpenApiInfo { Title = "t", Version = "1" },
			Paths = new OpenApiPaths
			{
				["/x"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new()
						{
							OperationId = operationId,
							Tags = new HashSet<OpenApiTagReference> { new("core") },
							Parameters = parameterNames.Select(
								name => (IOpenApiParameter)new OpenApiParameter { Name = name, In = ParameterLocation.Query }
							).ToList(),
							Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "ok" } }
						}
					}
				}
			}
		};

	private static OpenApiDocument SpecWithNestedRequestBody(string operationId, string parent, string nested)
	{
		var document = SpecWith(operationId);
		document.Paths["/x"].Operations![HttpMethod.Get].RequestBody = new OpenApiRequestBody
		{
			Content = new Dictionary<string, IOpenApiMediaType>
			{
				["application/json"] = new OpenApiMediaType
				{
					Schema = new OpenApiSchema
					{
						Properties = new Dictionary<string, IOpenApiSchema>
						{
							[parent] = new OpenApiSchema
							{
								Properties = new Dictionary<string, IOpenApiSchema>
								{
									[nested] = new OpenApiSchema { Type = JsonSchemaType.Object }
								}
							}
						}
					}
				}
			}
		};
		return document;
	}

	private sealed class CapturingDiagnosticsCollector() : DiagnosticsCollector([])
	{
		private readonly List<Diagnostic> _captured = [];

		public IEnumerable<string> ErrorMessages => _captured.Where(d => d.Severity == Severity.Error).Select(d => d.Message);

		public override void Write(Diagnostic diagnostic)
		{
			IncrementSeverityCount(diagnostic);
			_captured.Add(diagnostic);
		}

		public override DiagnosticsCollector StartAsync(Cancel ctx) => this;
		public override Task StopAsync(Cancel cancellationToken) => Task.CompletedTask;
	}
}
