// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Diagnostics;
using FluentAssertions;
using Xunit;

namespace Elastic.Markdown.Tests.AppliesTo;

public class VersionWarningTests
{
	[Fact]
	public void ValidVersion_NoWarning()
	{
		var diagnostics = new List<(Severity, string)>();
		var success = SemVersionConverter.TryParse("9.1.0", diagnostics, out var version);

		success.Should().BeTrue();
		version.Should().NotBeNull();
		version!.ToString().Should().Be("9.1.0");
		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public void ValidVersionWithFallback_NoWarning()
	{
		var diagnostics = new List<(Severity, string)>();
		var success = SemVersionConverter.TryParse("9.1", diagnostics, out var version);

		success.Should().BeTrue();
		version.Should().NotBeNull();
		version!.ToString().Should().Be("9.1.0");
		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public void InvalidVersionWithOperator_WarningEmitted()
	{
		var diagnostics = new List<(Severity, string)>();
		var success = SemVersionConverter.TryParse(">=9.1.0", diagnostics, out var version);

		success.Should().BeFalse();
		version.Should().BeNull();
		diagnostics.Should().HaveCount(1);
		diagnostics[0].Item1.Should().Be(Severity.Warning);
		diagnostics[0].Item2.Should().Contain("Invalid version format '>=9.1.0'");
		diagnostics[0].Item2.Should().Contain("Expected semantic version format");
	}

	[Fact]
	public void InvalidVersionWithBacktick_WarningEmitted()
	{
		var diagnostics = new List<(Severity, string)>();
		var success = SemVersionConverter.TryParse("9.1`", diagnostics, out var version);

		success.Should().BeFalse();
		version.Should().BeNull();
		diagnostics.Should().HaveCount(1);
		diagnostics[0].Item1.Should().Be(Severity.Warning);
		diagnostics[0].Item2.Should().Contain("Invalid version format '9.1`'");
		diagnostics[0].Item2.Should().Contain("Expected semantic version format");
	}

	[Fact]
	public void InvalidVersionFormat_WarningEmitted()
	{
		var diagnostics = new List<(Severity, string)>();
		var success = SemVersionConverter.TryParse("invalid-version", diagnostics, out var version);

		success.Should().BeFalse();
		version.Should().BeNull();
		diagnostics.Should().HaveCount(1);
		diagnostics[0].Item1.Should().Be(Severity.Warning);
		diagnostics[0].Item2.Should().Contain("Invalid version format 'invalid-version'");
	}

	[Fact]
	public void EmptyVersion_NoWarning()
	{
		var diagnostics = new List<(Severity, string)>();
		var success = SemVersionConverter.TryParse("", diagnostics, out var version);

		success.Should().BeTrue();
		version.Should().Be(AllVersions.Instance);
		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public void AllVersion_NoWarning()
	{
		var diagnostics = new List<(Severity, string)>();
		var success = SemVersionConverter.TryParse("all", diagnostics, out var version);

		success.Should().BeTrue();
		version.Should().Be(AllVersions.Instance);
		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public void NullVersion_NoWarning()
	{
		var diagnostics = new List<(Severity, string)>();
		var success = SemVersionConverter.TryParse(null, diagnostics, out var version);

		success.Should().BeTrue();
		version.Should().Be(AllVersions.Instance);
		diagnostics.Should().BeEmpty();
	}
}
