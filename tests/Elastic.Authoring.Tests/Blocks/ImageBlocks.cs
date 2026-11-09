// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using static Elastic.Authoring.Tests.Framework.TestFile;

namespace Elastic.Authoring.Tests.Blocks;

public class StaticPathToImage : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{image} img/observability.png
		:alt: Elasticsearch
		:width: 250px
		:screenshot:
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<a class="reference internal image-reference" href="javascript:void(0)" onclick="document.getElementById('modal-D9161EE2').style.display='flex'">
			<img loading="lazy" title="Elasticsearch" alt="Elasticsearch" src="/img/observability.png" style="width: 250px;" class="screenshot">
		</a>
		<div id="modal-D9161EE2" class="modal" onclick="document.querySelector('#modal-D9161EE2 .modal-content').contains(event.target) ? void(0) : document.getElementById('modal-D9161EE2').style.display='none'">
			<div class="modal-content">
				<span class="modal-close">
					<a href="javascript:void(0);" class="close-button" onclick="document.getElementById('modal-D9161EE2').style.display='none'">
						×
					</a>
				</span>
				<a class="reference internal image-reference" href="/img/observability.png" target="_blank">
					<img loading="lazy" title="Elasticsearch" alt="Elasticsearch" src="/img/observability.png">
				</a>
			</div>
		</div>
		"""
		);
}

public class SupportsUrlPathPrefix : GeneratorTest
{
	protected override SetupOptions Options => new() { UrlPathPrefix = "/docs" };

	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Static("img/observability.png"),
			Index(
				"""
			# Testing nested inline anchors
			:::{image} img/observability.png
			:alt: Elasticsearch
			:width: 250px
			:screenshot:
			:::
			"""
			),
			Page(
				"folder/relative.md",
				"""
			:::{image} ../img/observability.png
			:alt: Elasticsearch
			:width: 250px
			:screenshot:
			:::
			"""
			)
		];

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<a class="reference internal image-reference" href="javascript:void(0)" onclick="document.getElementById('modal-7F46B77E').style.display='flex'">
			<img loading="lazy" title="Elasticsearch" alt="Elasticsearch" src="/docs/img/observability.png" style="width: 250px;" class="screenshot">
		</a>
		<div id="modal-7F46B77E" class="modal" onclick="document.querySelector('#modal-7F46B77E .modal-content').contains(event.target) ? void(0) : document.getElementById('modal-7F46B77E').style.display='none'">
			<div class="modal-content">
				<span class="modal-close">
					<a href="javascript:void(0);" class="close-button" onclick="document.getElementById('modal-7F46B77E').style.display='none'">
						×
					</a>
				</span>
				<a class="reference internal image-reference" href="/docs/img/observability.png" target="_blank">
					<img loading="lazy" title="Elasticsearch" alt="Elasticsearch" src="/docs/img/observability.png">
				</a>
			</div>
		</div>
		"""
		);

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtmlOfFolderRelative() =>
		await Docs.Converts("folder/relative.md").ToHtml(
			"""
		<a class="reference internal image-reference" href="javascript:void(0)" onclick="document.getElementById('modal-7F46B77E').style.display='flex'">
			<img loading="lazy" title="Elasticsearch" alt="Elasticsearch" src="/docs/img/observability.png" style="width: 250px;" class="screenshot">
		</a>
		<div id="modal-7F46B77E" class="modal" onclick="document.querySelector('#modal-7F46B77E .modal-content').contains(event.target) ? void(0) : document.getElementById('modal-7F46B77E').style.display='none'">
			<div class="modal-content">
				<span class="modal-close">
					<a href="javascript:void(0);" class="close-button" onclick="document.getElementById('modal-7F46B77E').style.display='none'">
						×
					</a>
				</span>
				<a class="reference internal image-reference" href="/docs/img/observability.png" target="_blank">
					<img loading="lazy" title="Elasticsearch" alt="Elasticsearch" src="/docs/img/observability.png">
				</a>
			</div>
		</div>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class ImageRefOutOfScope : GeneratorTest
{
	protected override SetupOptions Options => new() { UrlPathPrefix = "/docs" };

	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Static("img/observability.png"),
			Index(
				"""
			# Testing nested inline anchors
			:::{image} ../img/observability.png
			:alt: Elasticsearch
			:width: 250px
			:screenshot:
			:::
			"""
			)
		];

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<a class="reference internal image-reference" href="javascript:void(0)" onclick="document.getElementById('modal-7F46B77E').style.display='flex'">
			<img loading="lazy" title="Elasticsearch" alt="Elasticsearch" src="/docs/img/observability.png" style="width: 250px;" class="screenshot">
		</a>
		<div id="modal-7F46B77E" class="modal" onclick="document.querySelector('#modal-7F46B77E .modal-content').contains(event.target) ? void(0) : document.getElementById('modal-7F46B77E').style.display='none'">
			<div class="modal-content">
				<span class="modal-close">
					<a href="javascript:void(0);" class="close-button" onclick="document.getElementById('modal-7F46B77E').style.display='none'">
						×
					</a>
				</span>
				<a class="reference internal image-reference" href="/docs/img/observability.png" target="_blank">
					<img loading="lazy" title="Elasticsearch" alt="Elasticsearch" src="/docs/img/observability.png">
				</a>
			</div>
		</div>
		"""
		);

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics(
			"""
			error {repo}/docs/index.md:2 `../img/observability.png` does not exist. resolved to `{repo}/img/observability.png`
			warning {repo}/docs/index.md:2 Image '../img/observability.png' is referenced out of table of contents scope '{repo}/docs'.
			"""
		);
}

public class ScreenshotClassInsideListInAppliesSwitch : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::::{applies-switch}
		::::{applies-item} serverless: ga

		1. Customize the lifecycle.

		   :::{image} img/observability.png
		   :alt: Elasticsearch
		   :width: 250px
		   :screenshot:
		   :::

		::::
		:::::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<div class="applies-switch">
			<input class="applies-switch-input" checked="checked" id="applies-switch-item-1-0" name="applies-switch-set-1" type="radio" tabindex="0">
			<label class="applies-switch-label" data-sync-id="applies-54C48F79" data-sync-group="applies-switches" for="applies-switch-item-1-0">
				<span class="applies applies-item">
					<applies-to-popover badge-key="Serverless" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="" show-popover="false" is-inline="true"></applies-to-popover>
				</span>
			</label>
			<div class="applies-switch-content" tabindex="0">
				<ol>
					<li>
						<p>Customize the lifecycle.</p>
						<a class="reference internal image-reference" href="javascript:void(0)" onclick="document.getElementById('modal-D9161EE2').style.display='flex'">
							<img loading="lazy" title="Elasticsearch" alt="Elasticsearch" src="/img/observability.png" style="width: 250px;" class="screenshot">
						</a>
						<div id="modal-D9161EE2" class="modal" onclick="document.querySelector('#modal-D9161EE2 .modal-content').contains(event.target) ? void(0) : document.getElementById('modal-D9161EE2').style.display='none'">
							<div class="modal-content">
								<span class="modal-close">
									<a href="javascript:void(0);" class="close-button" onclick="document.getElementById('modal-D9161EE2').style.display='none'">
										×
									</a>
								</span>
								<a class="reference internal image-reference" href="/img/observability.png" target="_blank">
									<img loading="lazy" title="Elasticsearch" alt="Elasticsearch" src="/img/observability.png">
								</a>
							</div>
						</div>
					</li>
				</ol>
			</div>
		</div>
		"""
		);
}

public class EmptyAltText : MarkdownTest
{
	protected override string Markdown => """
		:::{image} img/some-image.png
		:alt:
		:width: 250px
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<a class="reference internal image-reference" href="javascript:void(0)" onclick="document.getElementById('modal-7A740365').style.display='flex'">
			<img loading="lazy" alt="" src="/img/some-image.png" style="width: 250px;">
		</a>
		<div id="modal-7A740365" class="modal" onclick="document.querySelector('#modal-7A740365 .modal-content').contains(event.target) ? void(0) : document.getElementById('modal-7A740365').style.display='none'">
			<div class="modal-content">
				<span class="modal-close">
					<a href="javascript:void(0);" class="close-button" onclick="document.getElementById('modal-7A740365').style.display='none'">
						×
					</a>
				</span>
				<a class="reference internal image-reference" href="/img/some-image.png" target="_blank">
					<img loading="lazy" alt="" src="/img/some-image.png">
				</a>
			</div>
		</div>
		"""
		);
}
