// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.IO;
using FluentAssertions;

namespace Elastic.Markdown.Tests;

public class OutputDirectoryTests(ITestOutputHelper output)
{
	[Fact]
	public async Task CreatesDefaultOutputDirectory()
	{
		var logger = new TestLoggerFactory(output);
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/docset.yml",
				//language=yaml
				new MockFileData("""
project: test
toc:
- file: index.md
""") },
			{ "docs/index.md", new MockFileData("test") }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		await using var collector = new DiagnosticsCollector([]).StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, fileSystem, configurationContext);
		var linkResolver = new TestCrossLinkResolver();
		var set = new DocumentationSet(context, logger, linkResolver);
		var generator = new DocumentationGenerator(set, logger);

		await generator.GenerateAll(TestContext.Current.CancellationToken);
		await collector.StopAsync(TestContext.Current.CancellationToken);

		fileSystem.Directory.Exists(".artifacts").Should().BeTrue();
	}

	[Fact]
	public void FilesWithSnippetsInNameNotTreatedAsSnippets()
	{
		var logger = new TestLoggerFactory(output);
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/docset.yml",
				//language=yaml
				new MockFileData("""
project: test
toc:
- file: index.md
- file: top_snippets.md
""") },
			{ "docs/index.md", new MockFileData("# Test") },
			{ "docs/top_snippets.md", new MockFileData("# Top Snippets") }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		var collector = new TestDiagnosticsCollector(output);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, fileSystem, configurationContext);
		var linkResolver = new TestCrossLinkResolver();
		var set = new DocumentationSet(context, logger, linkResolver);

		set.MarkdownFiles.Should().Contain(f => f.RelativePath.EndsWith("top_snippets.md"));
	}

	[Theory]
	[MemberData(nameof(ValidFileNames))]
	public void OutputFileValidationValidNames(string fileName)
	{
		var valid = DocumentationGenerator.IsValidFileName(fileName);
		valid.Should().BeTrue($"'{fileName}' should be a valid filename");
	}

	[Theory]
	[MemberData(nameof(InvalidFileNames))]
	public void OutputFileValidationInvalidNames(string fileName)
	{
		var valid = DocumentationGenerator.IsValidFileName(fileName);
		valid.Should().BeFalse($"'{fileName}' should be an invalid filename");
	}

	public static TheoryData<string> ValidFileNames =>
	[
		"test.md",
		"file.txt",
		"index.html",
		"readme.rst",

		// With numbers
		"test123.md",
		"123test.md",
		"file2.md",
		"99bottles.md",

		// With underscores
		"test_file.md",
		"my_long_file_name.md",
		"_leading_underscore.md",
		"trailing_underscore_.md",

		// With hyphens
		"test-file.md",
		"my-long-file-name.md",
		"trailing-hyphen-.md",

		// Combined underscores and hyphens
		"test_file-name.md",
		"my-file_name.md",

		// With dots in filename (before extension)
		"test.config.md",
		"file.test.backup.md",
		"v1.0.0.md",

		// With spaces (allowed per regex)
		"test file.md",
		"my document.md",

		// Paths with all lowercase directories
		"path/to/file.md",
		"deep/nested/path/to/file.md",
		"folder/subfolder/document.md",

		// Paths with numbers
		"path123/file.md",
		"v1/docs/guide.md",

		// Paths with underscores and hyphens
		"my_folder/file.md",
		"my-folder/file.md",
		"path_to/sub-folder/file.md",

		// SVG files exception (even with uppercase - per the .EndsWith checks)
		"image.svg",
		"Icon.svg",
		"LOGO.svg",
		"path/to/Image.svg",

		// PNG files exception
		"image.png",
		"Screenshot.png",
		"IMAGE.png",
		"path/to/Logo.png",

		// GIF files exception
		"animation.gif",
		"Loading.gif",
		"SPINNER.gif",

		// ESQL snippets exception (prior art)
		"reference/query-languages/esql/_snippets/functions/examples/cbrt.md",
		"reference/query-languages/esql/_snippets/anything/here/File.md",
		"reference/query-languages/esql/_snippets/UPPERCASE.md",

		// Hardcoded exceptions
		"reference/security/prebuilt-rules/audit_policies/windows/README.md",
		"extend/integrations/developer-workflow-fleet-UI.md",
		"reference/elasticsearch/clients/ruby/Helpers.md",
		"explore-analyze/ai-features/llm-guides/connect-to-vLLM.md",

		// Plus sign in path (e.g., semantic version with build metadata)
		"release-notes/_snippets/9.2.0+build202510300150/index.md",
		"test+file.md",
		"c++.md"
	];

	public static TheoryData<string> InvalidFileNames =>
	[
		"Test.md",
		"FILE.md",
		"MyFile.md",
		"testFile.md",
		"README.md",

		// Uppercase in extension
		"test.MD",
		"test.Md",
		"file.TXT",
		"document.Html",

		// Uppercase in directory path
		"Path/file.md",
		"path/To/file.md",
		"FOLDER/file.md",
		"docs/MyFolder/file.md",

		// Filenames starting with invalid characters (must start with [a-z0-9_])
		"-leading-hyphen.md",
		"-file.md",
		".hidden.md",
		" leading-space.md",
		"path/to/-invalid.md",
		"path/to/.hidden.md",
		"path/to/ space.md",

		// Special characters - parentheses
		"test(1).md",
		"file (copy).md",
		"document(v2).md",

		// Special characters - square brackets
		"test[1].md",
		"file[copy].md",

		// Special characters - curly braces
		"test{1}.md",

		// Special characters - exclamation mark
		"test!.md",
		"important!file.md",

		// Special characters - at sign
		"test@file.md",
		"user@domain.md",

		// Special characters - hash
		"test#1.md",
		"file#.md",

		// Special characters - dollar sign
		"test$file.md",
		"price$.md",

		// Special characters - percent
		"test%file.md",
		"100%done.md",

		// Special characters - caret
		"test^file.md",

		// Special characters - ampersand
		"test&file.md",
		"this&that.md",

		// Special characters - asterisk
		"test*file.md",
		"*.md",

		// Special characters - equals sign
		"test=file.md",

		// Special characters - pipe
		"test|file.md",

		// Special characters - less than / greater than
		"test<file>.md",

		// Special characters - colon
		"test:file.md",

		// Special characters - semicolon
		"test;file.md",

		// Special characters - single quote
		"test'file.md",
		"it's.md",

		// Special characters - double quote
		"test\"file.md",

		// Special characters - backtick
		"test`file.md",

		// Special characters - tilde
		"test~file.md",
		"~temp.md",

		// Special characters - comma
		"test,file.md",
		"a,b,c.md",

		// Special characters - question mark
		"test?.md",
		"what?.md",

		// No extension
		"testfile",
		"README",
		"Makefile",

		// Just extension
		".md",
		".txt",

		// Empty extension
		"test.",

		// Double extension edge cases with uppercase
		"test.Config.md",
		"file.Test.md",

		// Non-ASCII characters - accented
		"tëst.md",
		"café.md",
		"naïve.md",
		"résumé.md",

		// Non-ASCII characters - other alphabets
		"тест.md",
		"测试.md",
		"テスト.md",

		// Non-ASCII characters - symbols
		"test™.md",
		"file©.md",

		// Empty string
		"",

		// Whitespace only
		"   ",

		// Extension only variations
		"..md",

		// Numbers in extension (if we expect only letters)
		"test.md5",
		"file.mp3",
		"video.mp4",

		// CamelCase variations
		"camelCase.md",
		"PascalCase.md",
		"mixedCASE.md",

		// Acronyms
		"API.md",
		"HTTP.md",
		"XMLParser.md",

		// Common problematic filenames
		"CHANGELOG.md",
		"LICENSE.md",
		"CONTRIBUTING.md",
		"TODO.md"
	];
}
