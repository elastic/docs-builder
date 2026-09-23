// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Container.DefinitionLists;

public class SimpleMultilineDefinitionWithMarkup : MarkdownTest
{
	protected override string Markdown =>
		"""
		This is my `definition`
		:   And this is the definition **body**

		    Which may contain multiple lines
		""";

	[Fact(DisplayName = "validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<dl>
		    <dt>This is my <code>definition</code> </dt>
		    <dd>
		        <p> And this is the definition <strong>body</strong></p>
		        <p>Which may contain multiple lines</p>
		    </dd>
		</dl>
		"""
		);

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class DefinitionWithEmbeddedDirectives : MarkdownTest
{
	protected override string Markdown =>
		"""
		This is my `definition`
		:   And this is the definition **body**
		    Which may contain multiple lines
		    :::{note}
		    My note
		    :::
		""";

	[Fact(DisplayName = "validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<dl>
			<dt>This is my
				<code>definition</code>
			</dt>
			<dd>
				<p>And this is the definition
					<strong>body</strong>
					Which may contain multiple lines</p>
				<div class="admonition note">
					<div class="admonition-header">
						<span class="admonition-title">Note</span>
					</div>
					<div class="admonition-content">
						<p>My note</p>
					</div>
				</div>
			</dd>
		</dl>
		"""
		);

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class DefinitionPreservesParagraphs : MarkdownTest
{
	protected override string Markdown =>
		"""
		Elastic Consumption Unit (ECU)
		:   An ECU is a unit of aggregate consumption across multiple resources over time.

		    Each type of computing resource (capacity, data transfer, and snapshot) that you consume has its own unit of measure.

		    In order to aggregate consumption across different resource types, all resources are priced in ECU.

		    Check Using Elastic Consumption Units for billing for more details.
		""";

	[Fact(DisplayName = "validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<dl>
		    <dt>Elastic Consumption Unit (ECU)</dt>
		    <dd>
		        <p>An ECU is a unit of aggregate consumption across multiple resources over time.</p>
		        <p>Each type of computing resource (capacity, data transfer, and snapshot) that you consume has its own unit of measure.</p>
		        <p>In order to aggregate consumption across different resource types, all resources are priced in ECU.</p>
		        <p>Check Using Elastic Consumption Units for billing for more details.</p>
		    </dd>
		</dl>
		"""
		);

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}
