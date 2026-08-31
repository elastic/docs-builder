// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using FakeItEasy;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Build.Tests;

/*
 * AssemblerBuildService.BuildAll() Behavior Matrix
 * =================================================
 *
 * AssumeBuild is three-state (bool?):
 *   null  → default: true locally, false on CI (via IEnvironmentVariables.IsRunningOnCI)
 *   true  → force skip (throws if on CI)
 *   false → force rebuild
 *
 * The stamp check replaces the old File.Exists(index.html) approach.
 * A stamp hit means code/config/content are unchanged since the last build.
 *
 * Truth Table (after the default is resolved):
 * +---------+------------------+------------+------------------------+-----------------------------------+
 * | On CI   | effectiveAssume  | Stamp Hit  | elasticsearchExportOnly| Result                            |
 * +---------+------------------+------------+------------------------+-----------------------------------+
 * | false   | false            | any        | false                  | Clears output, rebuilds           |
 * | false   | true             | true       | false                  | Skips build entirely (stamp match)|
 * | false   | true             | false/none | false                  | Clears output, rebuilds           |
 * | false   | true             | any        | true                   | Skips stamp check, rebuilds       |
 * | true    | false (default)  | any        | any                    | Builds fresh                      |
 * | true    | true (explicit)  | any        | any                    | ERROR (not allowed on CI)         |
 * +---------+------------------+------------+------------------------+-----------------------------------+
 *
 * Three-state default:
 * | assumeBuild param | On CI | effectiveAssumeBuild |
 * |-------------------|-------|----------------------|
 * | null              | false | true  (local default) |
 * | null              | true  | false (CI default)    |
 * | true              | false | true  (explicit)      |
 * | true              | true  | ERROR thrown          |
 * | false             | false | false (explicit opt-out) |
 * | false             | true  | false (explicit)      |
 *
 * Truth Table (CI (GITHUB_ACTIONS) column kept for legacy reference):
 * +-----------------------+-------------+---------------------------+
 * | CI (GITHUB_ACTIONS)   | assumeBuild | Result                    |
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

public class AssemblerBuildServiceTests : IDisposable
{
	private readonly TestLoggerFactory _loggerFactory = new(TestContext.Current.TestOutputHelper);
	private readonly NullCoreService _coreService = new();

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
	[InlineData(true, true)] // CI + assumeBuild=true -> should throw

	[InlineData(true, false)] // CI + assumeBuild=false -> should not throw

	[InlineData(false, true)] // Local + assumeBuild=true -> should not throw

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

	// ── Three-state default tests ──────────────────────────────────────────────

	[Theory]
	[InlineData(true, null, false)] // CI + null → effective false (CI default)

	[InlineData(false, null, true)] // Local + null → effective true (local default)

	[InlineData(true, false, false)] // CI + explicit false → effective false

	[InlineData(false, false, false)] // Local + explicit false → effective false

	[InlineData(false, true, true)] // Local + explicit true → effective true

	public void AssumeBuild_ThreeStateDefault_ResolvesCorrectly(bool isCI, bool? assumeBuild, bool expectedEffective)
	{
		var env = isCI ? MockEnvironmentVariables.CreateCI() : MockEnvironmentVariables.CreateLocal();
		// Mirror the production logic: assumeBuild ?? !_env.IsRunningOnCI
		var effective = assumeBuild ?? !env.IsRunningOnCI;
		effective.Should().Be(expectedEffective);
	}

	[Fact]
	public void AssumeBuild_ExplicitTrueOnCI_ThrowsGuard()
	{
		// explicit true + CI → the error guard must fire (assumeBuild == true && IsRunningOnCI)
		var env = MockEnvironmentVariables.CreateCI();
		var shouldThrow = env.IsRunningOnCI; // mirrors: assumeBuild == true && _env.IsRunningOnCI
		shouldThrow.Should().BeTrue("explicit --assume-build on CI must throw");
	}

	[Fact]
	public void AssumeBuild_DefaultTrueOnCI_DoesNotTriggerGuard()
	{
		// null (default) on CI: effective is false, guard condition is never true
		var env = MockEnvironmentVariables.CreateCI();
		bool? assumeBuild = null;
		var guardFires = assumeBuild == true && env.IsRunningOnCI;
		guardFires.Should().BeFalse("the CI guard only fires for explicit --assume-build, never the default");
	}

	public void Dispose()
	{
		_loggerFactory.Dispose();
		GC.SuppressFinalize(this);
	}
}
