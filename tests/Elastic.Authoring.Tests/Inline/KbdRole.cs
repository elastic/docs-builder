// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Inline;

public class RendersSingleKbdRole : MarkdownTest
{
	protected override string Markdown => """
		{kbd}`cmd`
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml("""
		<p><kbd class="kbd" aria-label="Command"><span class="kbd-icon">⌘</span>Cmd</kbd></p>
		""");
}

public class RendersSingleCharacterKbdRole : MarkdownTest
{
	protected override string Markdown => """
		{kbd}`c`
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() => await Docs.ConvertsToHtml("""
		<p><kbd class="kbd">c</kbd></p>
		""");
}

public class RendersCombinedKbdRole : MarkdownTest
{
	protected override string Markdown => """
		{kbd}`cmd+shift+c`
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p><kbd class="kbd" aria-label="Command"><span class="kbd-icon">⌘</span>Cmd</kbd> + <kbd class="kbd"><span class="kbd-icon">⇧</span>Shift</kbd> + <kbd class="kbd">c</kbd></p>
		"""
		);
}

public class RendersCombinedKbdRoleWithSpecialCharacters : MarkdownTest
{
	protected override string Markdown => """
		{kbd}`ctrl+alt+del`
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p><kbd class="kbd" aria-label="Control"><span class="kbd-icon">⌃</span>Ctrl</kbd> + <kbd class="kbd"><span class="kbd-icon">⌥</span>Alt</kbd> + <kbd class="kbd" aria-label="Delete">Del</kbd></p>
		"""
		);
}

public class RendersAlternativeKbdRole : MarkdownTest
{
	protected override string Markdown => """
		{kbd}`ctrl|cmd+c`
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p>
		  <kbd class="kbd" aria-label="Control or Command">
		    <span class="kbd-icon">⌃</span>Ctrl
		    <span class="kbd-separator"></span>
		    <span class="kbd-icon">⌘</span>Cmd
		  </kbd>
		   +
		  <kbd class="kbd">c</kbd>
		</p>
		"""
		);
}

public class RendersKbdPlus : MarkdownTest
{
	protected override string Markdown => """
		{kbd}`plus`
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() => await Docs.ConvertsToHtml("""
		<p><kbd class="kbd">+</kbd></p>
		""");
}

public class RendersKbdPipe : MarkdownTest
{
	protected override string Markdown => """
		{kbd}`pipe`
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() => await Docs.ConvertsToHtml("""
		<p><kbd class="kbd" aria-label="Pipe">|</kbd></p>
		""");
}
