// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Tests.Diagnostics;

public class FilelessDiagnosticClassifierTests
{
	[Fact]
	public void LooksLikeException_ErrorWithExceptionDump_IsTrue()
	{
		var diagnostic = new Diagnostic
		{
			Severity = Severity.Error,
			File = "",
			Message = "IO error creating changelog: disk full" + Environment.NewLine + "System.IO.IOException: disk full"
		};

		FilelessDiagnosticClassifier.LooksLikeException(diagnostic).Should().BeTrue();
	}

	[Fact]
	public void LooksLikeException_OrdinaryError_IsFalse()
	{
		var diagnostic = new Diagnostic
		{
			Severity = Severity.Error,
			File = "",
			Message = "Product 'cloud-serverless' specifies version(s) '2026-08-27'"
		};

		FilelessDiagnosticClassifier.LooksLikeException(diagnostic).Should().BeFalse();
	}

	[Fact]
	public void LooksLikeException_WarningContainingExceptionWord_IsFalse()
	{
		var diagnostic = new Diagnostic
		{
			Severity = Severity.Warning,
			File = "",
			Message = "No changelog file found for PR: https://github.com/elastic/kibana/pull/279825"
		};

		FilelessDiagnosticClassifier.LooksLikeException(diagnostic).Should().BeFalse();
	}

	[Fact]
	public void Group_SplitsFilelessDiagnosticsBySeverityAndException()
	{
		var exception = new Diagnostic
		{
			Severity = Severity.Error,
			File = "",
			Message = "Unhandled service exception: boom" + Environment.NewLine + "System.InvalidOperationException: boom"
		};
		var error = new Diagnostic { Severity = Severity.Error, File = "", Message = "At least one product is required" };
		var warning = new Diagnostic { Severity = Severity.Warning, File = "", Message = "No changelog file found for PR: 1" };
		var hint = new Diagnostic { Severity = Severity.Hint, File = "", Message = "[-exclude] Skipping changelog creation" };
		var fileError = new Diagnostic { Severity = Severity.Error, File = "doc.md", Message = "broken link" };

		var groups = FilelessDiagnosticClassifier.Group([exception, error, warning, hint, fileError]);

		groups.Exceptions.Should().ContainSingle().Which.Message.Should().Contain("InvalidOperationException:");
		groups.Errors.Should().ContainSingle().Which.Message.Should().Contain("product is required");
		groups.Warnings.Should().ContainSingle().Which.Message.Should().Contain("No changelog file found");
		groups.Hints.Should().ContainSingle().Which.Message.Should().Contain("Skipping changelog creation");
		groups.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void Group_EmptyWhenAllDiagnosticsAreFileAnchored()
	{
		var groups = FilelessDiagnosticClassifier.Group([
			new Diagnostic { Severity = Severity.Error, File = "a.md", Message = "err" },
			new Diagnostic { Severity = Severity.Warning, File = "b.md", Message = "warn" }
		]);

		groups.IsEmpty.Should().BeTrue();
	}
}
