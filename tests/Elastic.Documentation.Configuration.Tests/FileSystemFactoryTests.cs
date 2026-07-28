// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class FileSystemFactoryTests
{
	[Fact]
	public void ScopeCurrentWorkingDirectory_NestedExtensionRoot_DoesNotThrow()
	{
		var workingRoot = Paths.WorkingDirectoryRoot.FullName;
		var nestedConfigDir = Path.Join(workingRoot, "environments", "internal");
		var configPath = Path.Join(nestedConfigDir, "config.yml");
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ configPath, new MockFileData("environment: internal") }
		});

		var act = () => FileSystemFactory.ScopeCurrentWorkingDirectory(mockFs, [nestedConfigDir]);

		act.Should().NotThrow();
		var scoped = FileSystemFactory.ScopeCurrentWorkingDirectory(mockFs, [nestedConfigDir]);
		scoped.File.Exists(configPath).Should().BeTrue();
	}

	[Fact]
	public void ScopeCurrentWorkingDirectory_ExternalExtensionRoot_AllowsReadingExternalConfig()
	{
		var externalRoot = Path.Join(Path.GetTempPath(), $"external-codex-{Guid.NewGuid():N}");
		var configPath = Path.Join(externalRoot, "codex.yml");
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ configPath, new MockFileData("environment: internal") }
		});

		var scoped = FileSystemFactory.ScopeCurrentWorkingDirectory(mockFs, [externalRoot]);

		scoped.File.Exists(configPath).Should().BeTrue();
	}
}
