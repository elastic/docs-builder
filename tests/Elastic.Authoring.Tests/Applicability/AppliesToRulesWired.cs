// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Authoring.Tests.Applicability;

// ApplicableToReadConstraints runs once on every applies_to block read from a file; each problem is reported by one emitter under its rule's id.
// The helper fails on a second report of the problem, whatever its wording (see ErrorCollectorAssertions.HasRuleDiagnosticOnce).

public class DeprecatedLifecycleInDirectiveIsReportedByM91Once : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: coming 9.0
		```
		""";

	[Test, DisplayName("the rule reports it once, naming the lifecycle as written")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			ApplicableToConstraints.ApplicabilityLifecycleNotDeprecated.Name,
			Severity.Hint,
			"The 'coming' lifecycle is deprecated and will be removed in a future release"
		);
}

public class DeprecatedLifecycleInRoleIsReportedByM91Once : MarkdownTest
{
	protected override string Markdown => "This is an inline {applies_to}`stack: planned 9.1` element.";

	[Test, DisplayName("the rule reports it once")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			ApplicableToConstraints.ApplicabilityLifecycleNotDeprecated.Name,
			Severity.Hint,
			"The 'planned' lifecycle is deprecated"
		);
}

public class DeprecatedLifecycleInFrontMatterIsReportedByM91Once : DocumentTest
{
	protected override string Document =>
		"""
		---
		applies_to:
		  stack: discontinued 9.0
		---
		# Document
		""";

	[Test, DisplayName("the rule reports it once")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			ApplicableToConstraints.ApplicabilityLifecycleNotDeprecated.Name,
			Severity.Hint,
			"The 'discontinued' lifecycle is deprecated"
		);
}

public class DeprecatedLifecycleInAdmonitionIsReportedByM91Once : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{warning}
		:applies_to: stack: planned 9.0
		Careful.
		:::
		""";

	[Test, DisplayName("the rule reports it once")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			ApplicableToConstraints.ApplicabilityLifecycleNotDeprecated.Name,
			Severity.Hint,
			"The 'planned' lifecycle is deprecated"
		);
}

public class UnavailableLifecycleIsNowReportedByM91 : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: unavailable 9.0
		```
		""";

	[Test, DisplayName("the rule reports it once (the reader never did)")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			ApplicableToConstraints.ApplicabilityLifecycleNotDeprecated.Name,
			Severity.Hint,
			"The 'unavailable' lifecycle is deprecated"
		);
}

public class VersionedVersionlessKeyIsReportedOnceUnderM94 : DocumentTest
{
	protected override string Document =>
		"""
		---
		applies_to:
		  deployment:
		    ess: ga 9.0
		---
		# Document
		""";

	[Test, DisplayName("the reader's finding carries the rule's id and appears once")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			ApplicableToConstraints.VersionlessKeysHaveNoVersion.Name,
			Severity.Error,
			"Can't specify a version for 'ess'"
		);
}

public class VersionedServerlessStringIsReportedOnceUnderM94 : DocumentTest
{
	protected override string Document =>
		"""
		---
		applies_to:
		  serverless: ga 9.0
		---
		# Document
		""";

	[Test, DisplayName("the key as written, once")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			ApplicableToConstraints.VersionlessKeysHaveNoVersion.Name,
			Severity.Error,
			"Can't specify a version for 'serverless'"
		);
}

public class OverlappingVersionsAreReportedOnceUnderM96 : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: ga 9.0-9.3, beta =9.2
		```
		""";

	[Test, DisplayName("the reader's hint carries the rule's id and appears once")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			ApplicableToConstraints.ApplicabilityVersionsConsistent.Name,
			Severity.Hint,
			"Overlapping version ranges detected"
		);
}

public class UnparsableVersionIsReportedByM92 : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: ga 9.x
		```
		""";

	[Test, DisplayName("the version that could not be read is a warning, once")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			ApplicableToConstraints.ApplicabilityVersionReadable.Name,
			Severity.Warning,
			"the version '9.x' in 'ga 9.x' could not be parsed"
		);
}

public class UnreadNestedKeyIsReportedByM89 : MarkdownTest
{
	protected override string Markdown =>
		"""
		```{applies_to}
		deployment:
		  ece: ga
		  bogus: ga
		```
		""";

	[Test, DisplayName("the key no reader maps is a warning, once")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			ApplicableToConstraints.AppliesToKeysRead.Name,
			Severity.Warning,
			"The applies_to key 'deployment.bogus' is not read"
		);
}

public class NonTextValueIsReportedByM93 : MarkdownTest
{
	protected override string Markdown =>
		"""
		```{applies_to}
		stack: ga 9.0
		serverless:
		  elasticsearch:
		    detail: ga
		```
		""";

	[Test, DisplayName("the value that is ignored is a warning, once")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			ApplicableToConstraints.AppliesToValuesAreText.Name,
			Severity.Warning,
			"The applies_to key 'serverless.elasticsearch' must have a text value"
		);
}

public class ReaderFindingStaysUnderM98AndIsNotRepeated : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		hosted: all
		```
		""";

	[Test, DisplayName("the unsupported key is reported once, as the reader words it, and the block is not also reported as empty")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			ApplicableToConstraints.ApplicabilityDiagnosticsReported.Name,
			Severity.Warning,
			"Applies block does not support the following keys: hosted"
		);
}
