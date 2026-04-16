// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer.Templates;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Toc;
using FakeItEasy;

namespace Elastic.ApiExplorer.Tests;

public class TemplateProcessorTests
{
	private readonly IMarkdownStringRenderer _mockRenderer;
	private readonly TemplateProcessor _processor;
	private readonly MockFileSystem _fileSystem;

	public TemplateProcessorTests()
	{
		_mockRenderer = A.Fake<IMarkdownStringRenderer>();
		_processor = new TemplateProcessor(_mockRenderer);
		_fileSystem = new MockFileSystem();
	}

	[Fact]
	public async Task ProcessTemplateAsync_WithCustomTemplate_ProcessesContent()
	{
		// Arrange
		var templateContent = "# API Overview\n\nThis is a custom template.";
		var expectedHtml = "<h1>API Overview</h1><p>This is a custom template.</p>";

		var templateFile = _fileSystem.FileInfo.New("/path/to/template.md");
		_fileSystem.AddFile(templateFile.FullName, new MockFileData(templateContent));

		var apiConfig = new ResolvedApiConfiguration
		{
			ProductKey = "kibana",
			TemplateFile = templateFile,
			SpecFiles = [_fileSystem.FileInfo.New("/path/to/spec.json")]
		};

		A.CallTo(() => _mockRenderer.Render(templateContent, templateFile))
			.Returns(expectedHtml);

		// Act
		var result = await _processor.ProcessTemplateAsync(apiConfig, TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be(expectedHtml);
		A.CallTo(() => _mockRenderer.Render(templateContent, templateFile)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task ProcessTemplateAsync_WithoutCustomTemplate_ReturnsEmpty()
	{
		// Arrange
		var apiConfig = new ResolvedApiConfiguration
		{
			ProductKey = "kibana",
			TemplateFile = null,
			SpecFiles = [_fileSystem.FileInfo.New("/path/to/spec.json")]
		};

		// Act
		var result = await _processor.ProcessTemplateAsync(apiConfig, TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be(string.Empty);
		A.CallTo(() => _mockRenderer.Render(A<string>._, A<IFileInfo>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task ProcessTemplateAsync_WithCancellation_ThrowsOperationCancelledException()
	{
		// Arrange
		var templateFile = _fileSystem.FileInfo.New("/path/to/template.md");
		_fileSystem.AddFile(templateFile.FullName, new MockFileData("# Test"));

		var apiConfig = new ResolvedApiConfiguration
		{
			ProductKey = "kibana",
			TemplateFile = templateFile,
			SpecFiles = [_fileSystem.FileInfo.New("/path/to/spec.json")]
		};

		using var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act & Assert
		var act = () => _processor.ProcessTemplateAsync(apiConfig, cts.Token);
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public void TemplateProcessorFactory_Create_ReturnsTemplateProcessor()
	{
		// Arrange
		var mockRenderer = A.Fake<IMarkdownStringRenderer>();

		// Act
		var processor = TemplateProcessorFactory.Create(mockRenderer);

		// Assert
		processor.Should().NotBeNull();
		processor.Should().BeOfType<TemplateProcessor>();
	}
}
