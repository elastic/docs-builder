// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using AwesomeAssertions;
using Elastic.Documentation.Integrations.S3;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Integrations.Tests.S3;

public class S3EtagCalculatorTests
{
	private readonly MockFileSystem _fileSystem = new();
	private readonly S3EtagCalculator _calculator;

	public S3EtagCalculatorTests() =>
		_calculator = new S3EtagCalculator(NullLoggerFactory.Instance, _fileSystem);

	private string TempPath(string name) =>
		_fileSystem.Path.Join(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), name);

	[Fact]
	[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
	public async Task CalculateS3ETag_SmallFile_ReturnsMd5Hex()
	{
		var content = "hello changelog"u8.ToArray();
		var path = TempPath("test.yaml");
		_fileSystem.AddFile(path, new MockFileData(content));

		var expected = Convert.ToHexStringLower(MD5.HashData(content));
		var ct = TestContext.Current.CancellationToken;

		var etag = await _calculator.CalculateS3ETag(path, ct);

		etag.Should().Be(expected);
	}

	[Fact]
	[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
	public async Task CalculateS3ETag_EmptyFile_ReturnsMd5OfEmpty()
	{
		var path = TempPath("empty.yaml");
		_fileSystem.AddFile(path, new MockFileData([]));

		var expected = Convert.ToHexStringLower(MD5.HashData([]));
		var ct = TestContext.Current.CancellationToken;

		var etag = await _calculator.CalculateS3ETag(path, ct);

		etag.Should().Be(expected);
	}

	[Fact]
	public async Task CalculateS3ETag_SameFileTwice_ReturnsCachedResult()
	{
		var path = TempPath("cached.yaml");
		_fileSystem.AddFile(path, new MockFileData("cached content"u8.ToArray()));

		var ct = TestContext.Current.CancellationToken;
		var first = await _calculator.CalculateS3ETag(path, ct);
		var second = await _calculator.CalculateS3ETag(path, ct);

		first.Should().Be(second);
	}

	[Fact]
	public async Task CalculateS3ETag_DifferentFiles_ReturnDifferentEtags()
	{
		var pathA = TempPath("a.yaml");
		var pathB = TempPath("b.yaml");
		_fileSystem.AddFile(pathA, new MockFileData("content a"u8.ToArray()));
		_fileSystem.AddFile(pathB, new MockFileData("content b"u8.ToArray()));

		var ct = TestContext.Current.CancellationToken;
		var etagA = await _calculator.CalculateS3ETag(pathA, ct);
		var etagB = await _calculator.CalculateS3ETag(pathB, ct);

		etagA.Should().NotBe(etagB);
	}
}
