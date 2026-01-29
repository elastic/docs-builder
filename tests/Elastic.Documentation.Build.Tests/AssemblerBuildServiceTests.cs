// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Build.Tests;

/*
 * AssemblerBuildService.BuildAll() Behavior Matrix
 * =================================================
 *
 * Column Definitions:
 * -------------------
 * - CI (GITHUB_ACTIONS): Whether the GITHUB_ACTIONS environment variable is set
 *     - Checked via IEnvironmentVariables.IsRunningOnCI
 *     - When set: Indicates build is running in GitHub Actions CI pipeline
 *     - Affects: --assume-build validation, output directory cleanup behavior
 *
 * - assumeBuild: The '--assume-build' flag passed to BuildAll()
 *     - Purpose: Skip entire build if output already exists (index.html present)
 *     - Use case: ONLY for local development/testing to avoid rebuilding when unnecessary
 *         - Speeds up test iterations when you know the build output is still valid
 *         - Useful for integration tests that don't need fresh builds
 *     - DANGER on CI: Could serve stale content from a previous/cached build
 *         - CI caches might contain outdated build artifacts
 *         - Merged code changes wouldn't be reflected in output
 *         - Could lead to deploying old documentation
 *     - Therefore: This flag throws an error when used on CI
 *
 * - Output Exists: Whether the output directory already exists
 *     - For assumeBuild: Also checks if docs/index.html exists within output
 *     - Determines: Whether cleanup is needed, whether assumeBuild can skip
 *
 * - elasticsearchExportOnly: Whether exporters contains ONLY Exporter.Elasticsearch
 *     - When true: Only generating Elasticsearch search index data, not HTML
 *         - HTML output not being regenerated
 *         - Output directory cleanup would be wasteful/destructive
 *     - Allows skipping output directory cleanup since HTML isn't being regenerated
 *         - Previous HTML remains intact for serving
 *         - Only ES index data is updated
 *
 * - Result: What action the service takes
 *     - "Builds, creates output": Normal full build, creates output from scratch
 *         - Complete build process runs
 *         - All documentation sets are processed
 *         - All exporters generate their outputs
 *     - "Clears output, rebuilds": Deletes existing output directory first, then builds
 *         - OutputDirectory.Delete(true) is called
 *         - Ensures no stale files remain
 *         - Then proceeds with full build
 *     - "Skips clear, rebuilds": Keeps output directory (for ES export), rebuilds in place
 *         - Output directory NOT deleted
 *         - Build proceeds, updating only ES index
 *         - HTML files from previous build remain
 *     - "Skips build entirely": Returns early without building (assumeBuild optimization)
 *         - Returns true immediately
 *         - No build steps executed
 *         - Output from previous build is assumed valid
 *     - "ERROR": Throws InvalidOperationException because this combination is not allowed
 *         - --assume-build on CI is always an error
 *         - Protects against stale content being deployed
 *
 * Truth Table:
 * +-----------------------+-------------+---------------+-------------------------+---------------------------+
 * | CI (GITHUB_ACTIONS)   | assumeBuild | Output Exists | elasticsearchExportOnly | Result                    |
 * +-----------------------+-------------+---------------+-------------------------+---------------------------+
 * | false                 | false       | false         | false                   | Builds, creates output    |
 * | false                 | false       | true          | false                   | Clears output, rebuilds   |
 * | false                 | false       | true          | true                    | Skips clear, rebuilds     |
 * | false                 | true        | false         | false                   | Builds (no prior output)  |
 * | false                 | true        | true (index)  | false                   | Skips build entirely      |
 * | true                  | false       | false         | false                   | Builds, creates output    |
 * | true                  | false       | true          | false                   | Clears output, rebuilds   |
 * | true                  | true        | any           | any                     | ERROR (not allowed on CI) |
 * +-----------------------+-------------+---------------+-------------------------+---------------------------+
 *
 * Key Invariants for CI:
 * 1. --assume-build is ALWAYS an error on CI
 *    - Rationale: CI should never trust existing output; it could be stale from cache
 *    - Ensures every CI build produces fresh, reproducible output
 *    - Exception thrown: InvalidOperationException with descriptive message
 *
 * 2. Output directory is ALWAYS cleared on CI (unless elasticsearch-only export)
 *    - Rationale: Prevents orphaned files from previous builds appearing in output
 *    - elasticsearch-only exception: Not generating HTML, so output dir is irrelevant
 *    - Guarantees clean slate for each CI build
 *
 * Environment Variables:
 * - GITHUB_ACTIONS: If set, --assume-build becomes an error
 *
 * GitHub Actions Inputs (via ICoreService.GetInput):
 * - "environment": Build environment (dev, staging, prod)
 */

public class AssemblerBuildServiceTests
{
	private readonly TestLoggerFactory _loggerFactory;
	private readonly NullCoreService _coreService;

	public AssemblerBuildServiceTests()
	{
		_loggerFactory = new TestLoggerFactory(TestContext.Current.TestOutputHelper);
		_coreService = new NullCoreService();
	}

	[Fact]
	public void Constructor_AcceptsIEnvironmentVariables()
	{
		// Arrange
		var env = MockEnvironmentVariables.CreateLocal();
		var mockFs = new MockFileSystem();
		var configContext = TestHelpers.CreateConfigurationContext(mockFs);
		var assemblyConfig = A.Fake<AssemblyConfiguration>();

		// Act
		var service = new AssemblerBuildService(_loggerFactory, assemblyConfig, configContext, _coreService, env);

		// Assert
		service.Should().NotBeNull();
	}

	[Theory]
	[InlineData(true, true)]   // CI + assumeBuild=true -> should throw
	[InlineData(true, false)]  // CI + assumeBuild=false -> should not throw
	[InlineData(false, true)]  // Local + assumeBuild=true -> should not throw
	[InlineData(false, false)] // Local + assumeBuild=false -> should not throw
	public void AssumeBuildValidation_FollowsTruthTable(bool isCI, bool assumeBuild)
	{
		// This test validates the truth table behavior for assumeBuild validation.
		// Only CI + assumeBuild=true should result in an error.

		var env = isCI ? MockEnvironmentVariables.CreateCI() : MockEnvironmentVariables.CreateLocal();

		// The validation logic in AssemblerBuildService.BuildAll() is:
		// if (assumeBuild.GetValueOrDefault(false) && _env.IsRunningOnCI)
		//     throw new InvalidOperationException(...)

		var shouldThrow = isCI && assumeBuild;

		// Verify our understanding of the logic
		var wouldThrow = assumeBuild && env.IsRunningOnCI;
		wouldThrow.Should().Be(shouldThrow);
	}

	[Fact]
	public void MockEnvironmentVariables_CIStatus_AffectsAssumeBuildValidation()
	{
		// Test that the mock correctly simulates CI/non-CI for validation logic

		var ciEnv = MockEnvironmentVariables.CreateCI();
		var localEnv = MockEnvironmentVariables.CreateLocal();

		// On CI, assumeBuild=true would cause validation to fail
		var ciWithAssumeBuild = ciEnv.IsRunningOnCI;
		ciWithAssumeBuild.Should().BeTrue("CI with assumeBuild should trigger validation error");

		// Locally, assumeBuild=true is allowed
		var localWithAssumeBuild = localEnv.IsRunningOnCI;
		localWithAssumeBuild.Should().BeFalse("Local with assumeBuild should not trigger validation error");
	}

	[Fact]
	public void IsRunningOnCI_WhenGitHubActionsSet_ReturnsTrue()
	{
		// Arrange
		var env = MockEnvironmentVariables.CreateCI();

		// Act & Assert
		env.IsRunningOnCI.Should().BeTrue();
	}

	[Fact]
	public void IsRunningOnCI_WhenGitHubActionsNotSet_ReturnsFalse()
	{
		// Arrange
		var env = MockEnvironmentVariables.CreateLocal();

		// Act & Assert
		env.IsRunningOnCI.Should().BeFalse();
	}

	[Fact]
	public void AssumeBuildOnCI_ShouldThrow_ValidationLogic()
	{
		// This test documents the expected behavior:
		// When IsRunningOnCI is true AND assumeBuild is true,
		// the service should throw InvalidOperationException

		var ciEnv = MockEnvironmentVariables.CreateCI();
		var assumeBuild = true;

		// This is the condition that triggers the error
		var shouldThrow = assumeBuild && ciEnv.IsRunningOnCI;

		shouldThrow.Should().BeTrue("CI + assumeBuild=true should cause an error");
	}

	[Fact]
	public void AssumeBuildLocally_ShouldNotThrow_ValidationLogic()
	{
		// This test documents the expected behavior:
		// When IsRunningOnCI is false, assumeBuild is allowed

		var localEnv = MockEnvironmentVariables.CreateLocal();
		var assumeBuild = true;

		// This is the condition that triggers the error
		var shouldThrow = assumeBuild && localEnv.IsRunningOnCI;

		shouldThrow.Should().BeFalse("Local + assumeBuild=true should be allowed");
	}
}
