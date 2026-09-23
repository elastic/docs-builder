// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using AwesomeAssertions;
using JetBrains.Annotations;

namespace Elastic.Authoring.Tests.Framework;

/// <summary>
/// Represents a single file placed under <c>docs/</c> in the mock file system.
/// The four static factories mirror the F# DU cases; all map identically to
/// <c>("docs/{path}", MockFileData(contents))</c> in the generator.
/// </summary>
public sealed record TestFile(string Path, string Contents)
{
	/// <summary>Creates <c>index.md</c> with the given Markdown content.</summary>
	public static TestFile Index([LanguageInjection("markdown")] string markdown) => new("index.md", markdown);

	/// <summary>
	/// Creates a Markdown file at the given relative path under <c>docs/</c>.
	/// Named <c>Page</c> (not <c>Markdown</c>) to avoid ambiguity with the <c>Elastic.Markdown</c>
	/// namespace inside the <c>Elastic.*</c> namespace hierarchy.
	/// </summary>
	public static TestFile Page(string path, [LanguageInjection("markdown")] string markdown) => new(path, markdown);

	/// <summary>Creates a snippet file (treated identically to a Markdown file by the generator).</summary>
	public static TestFile Snippet(string path, [LanguageInjection("markdown")] string markdown) => new(path, markdown);

	/// <summary>Creates an empty static file (e.g. an image placeholder).</summary>
	public static TestFile Static(string path) => new(path, string.Empty);
}

/// <summary>Configuration options for a documentation set in an authoring test.</summary>
public sealed record SetupOptions
{
	/// <summary>
	/// Optional URL path prefix, e.g. <c>"/docs"</c>. Maps to
	/// <see cref="Elastic.Documentation.Configuration.BuildContext.UrlPathPrefix"/>.
	/// </summary>
	public string? UrlPathPrefix { get; init; }

	/// <summary>
	/// Optional list of product IDs written into the <c>docset.yml</c> under <c>products:</c>.
	/// </summary>
	public IReadOnlyCollection<string>? DocsetProducts { get; init; }

	public static SetupOptions Empty { get; } = new();
}

/// <summary>
/// A lazily-built, once-per-test-class documentation generation.
/// Created by <see cref="Setup"/> factory methods and held by
/// <see cref="AuthoringTest.Docs"/> via a per-type cache.
///
/// All assertion methods await the generator run on first call and return immediately on
/// subsequent calls (the <see cref="Lazy{T}"/> ensures the factory runs exactly once,
/// and the <see cref="Task{T}"/> is already completed on subsequent awaits).
///
/// Deliberately NOT a static field initializer (which would produce an opaque
/// <see cref="TypeInitializationException"/> on failure). Instead, evaluated inside the
/// instance property getter of the concrete test class the first time any assertion runs.
/// </summary>
public sealed class Scenario
{
	// LazyThreadSafetyMode.ExecutionAndPublication: at most one factory invocation,
	// even if multiple test methods in the class start concurrently.
	private readonly Lazy<Task<GeneratorResults>> _results;

	internal Scenario(Func<Task<GeneratorResults>> build) =>
		_results = new Lazy<Task<GeneratorResults>>(build, LazyThreadSafetyMode.ExecutionAndPublication);

	/// <summary>
	/// Returns the generator task. Idempotent: the <see cref="Lazy{T}"/> guarantees
	/// the factory runs at most once per <see cref="Scenario"/> instance.
	/// </summary>
	private Task<GeneratorResults> BuildAsync() => _results.Value;

	// ────────────────────────────────────────────────────────────────────────────────
	// HTML assertions
	// ────────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Asserts that <c>index.md</c> renders to HTML that is structurally identical to
	/// <paramref name="expected"/> (after pretty-printing both sides).
	/// </summary>
	public async Task ConvertsToHtml([LanguageInjection("html")] string expected) =>
		HtmlAssertions.ConvertsToHtml(expected, await BuildAsync());

	/// <summary>
	/// Asserts that <c>index.md</c>'s rendered HTML contains the <paramref name="expected"/>
	/// fragment (pretty-printed substring match).
	/// </summary>
	public async Task ConvertsToContainingHtml([LanguageInjection("html")] string expected) =>
		HtmlAssertions.ConvertsToContainingHtml(expected, await BuildAsync());

	/// <summary>
	/// Asserts that <c>index.md</c>'s raw HTML string contains <paramref name="expected"/> verbatim.
	/// </summary>
	public async Task ConvertsToContainingRawHtml(string expected) =>
		HtmlAssertions.ConvertsToContainingRawHtml(expected, await BuildAsync());

	/// <summary>
	/// Asserts that <c>index.md</c>'s HTML does <em>not</em> contain <paramref name="expected"/>.
	/// </summary>
	public async Task DoesNotConvertToContainingHtml(string expected) =>
		HtmlAssertions.DoesNotConvertToContainingHtml(expected, await BuildAsync());

	// ────────────────────────────────────────────────────────────────────────────────
	// Diagnostics assertions
	// ────────────────────────────────────────────────────────────────────────────────

	/// <summary>Asserts the generator produced no errors.</summary>
	public async Task HasNoErrors() => ErrorCollectorAssertions.HasNoErrors(await BuildAsync());

	/// <summary>
	/// Asserts the generator produced at least one error whose message contains
	/// <paramref name="expected"/> (first-error-only semantics — see
	/// <see cref="ErrorCollectorAssertions"/>).
	/// </summary>
	public async Task HasError(string expected) => ErrorCollectorAssertions.HasError(expected, await BuildAsync());

	/// <summary>Asserts the generator produced no warnings.</summary>
	public async Task HasNoWarnings() => ErrorCollectorAssertions.HasNoWarnings(await BuildAsync());

	/// <summary>
	/// Asserts the generator produced at least one warning whose message contains
	/// <paramref name="expected"/> (first-warning-only semantics).
	/// </summary>
	public async Task HasWarning(string expected) => ErrorCollectorAssertions.HasWarning(expected, await BuildAsync());

	/// <summary>
	/// Asserts the generator produced at least one hint whose message contains
	/// <paramref name="expected"/> (first-hint-only semantics).
	/// </summary>
	public async Task HasHint(string expected) => ErrorCollectorAssertions.HasHint(expected, await BuildAsync());

	// ────────────────────────────────────────────────────────────────────────────────
	// Per-file result accessor
	// ────────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Returns a <see cref="MarkdownResultTask"/> for the file at <paramref name="path"/>
	/// in the generator results. Chain assertion methods off the returned object:
	/// <code>
	/// await Docs.Converts("folder/page.md").ContainsHtml("...");
	/// </code>
	/// Mirrors F# <c>converts</c>.
	/// </summary>
	public MarkdownResultTask Converts(string path) =>
		new(BuildAsync().ContinueWith(t => ResultsAssertions.Finds(path, t.Result), TaskScheduler.Default));

	// ────────────────────────────────────────────────────────────────────────────────
	// Applicability
	// ────────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Asserts that <c>index.md</c>'s <c>YamlFrontMatter.AppliesTo</c> equals
	/// <paramref name="expected"/> using structural <c>Equals</c>.
	/// </summary>
	public async Task AppliesTo(Elastic.Documentation.AppliesTo.ApplicableTo? expected) =>
		MarkdownDocumentAssertions.AppliesTo(expected, await BuildAsync());

	// ────────────────────────────────────────────────────────────────────────────────
	// Plain text
	// ────────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Asserts that <c>index.md</c>'s plain-text export equals <paramref name="expected"/>
	/// (both sides trimmed; diff shown on mismatch).
	/// </summary>
	public async Task ConvertsToPlainText([LanguageInjection("text")] string expected) =>
		PlainTextAssertions.ConvertsToPlainText(expected, await BuildAsync());

	// ────────────────────────────────────────────────────────────────────────────────
	// LLM Markdown
	// ────────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Asserts that <c>index.md</c>'s LLM Markdown export equals <paramref name="expected"/>
	/// (both sides trimmed; diff shown on mismatch).
	/// </summary>
	public async Task ConvertsToNewLlm([LanguageInjection("markdown")] string expected) =>
		LlmMarkdownAssertions.ConvertsToNewLlm(expected, await BuildAsync());

	/// <summary>
	/// Asserts that <c>index.md</c>'s LLM Markdown output including front-matter metadata
	/// equals <paramref name="expected"/> (see <see cref="LlmMarkdownAssertions"/> warning).
	/// </summary>
	public async Task ConvertsToLlmWithMetadata([LanguageInjection("markdown")] string expected) =>
		LlmMarkdownAssertions.ConvertsToLlmWithMetadata(expected, await BuildAsync());

	// ────────────────────────────────────────────────────────────────────────────────
	// JSON output
	// ────────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Asserts that the file at <paramref name="artifactPath"/> in the write file system
	/// contains JSON that normalizes to <paramref name="expected"/>.
	/// </summary>
	public async Task ConvertsToJson(string artifactPath, [LanguageInjection("json")] string expected) =>
		ResultsAssertions.ConvertsToJson(artifactPath, expected, await BuildAsync());
}

/// <summary>
/// Entry points for the authoring test harness, mirroring the F# <c>Setup</c> type.
/// </summary>
public static class Setup
{
	/// <summary>
	/// Wraps <paramref name="markdown"/> in a document with an H1 and places it in <c>index.md</c>.
	/// Mirrors <c>Setup.Markdown</c>.
	/// </summary>
	public static Scenario Markdown([LanguageInjection("markdown")] string markdown) => Document($"# Test Document\n{markdown}\n");

	/// <summary>
	/// Places <paramref name="markdown"/> verbatim in <c>index.md</c>.
	/// Mirrors <c>Setup.Document</c>.
	/// </summary>
	public static Scenario Document([LanguageInjection("markdown")] string markdown) => Generate([TestFile.Index(markdown)]);

	/// <summary>
	/// Builds a multi-file documentation set.
	/// Mirrors <c>Setup.Generate</c>.
	/// </summary>
	public static Scenario Generate(IReadOnlyCollection<TestFile> files) => Generate(SetupOptions.Empty, files);

	/// <summary>
	/// Builds a multi-file documentation set with custom options.
	/// Mirrors <c>Setup.GenerateWithOptions</c>.
	/// </summary>
	public static Scenario Generate(SetupOptions options, IReadOnlyCollection<TestFile> files) =>
		new(() => AuthoringGenerator.GenerateAsync(options, files));
}

/// <summary>
/// Base class for all authoring tests. Provides a <see cref="Docs"/> property that returns
/// the cached, lazily-built <see cref="Scenario"/> for this test class, plus helper accessors.
///
/// Subclasses define the documentation set under test by overriding <see cref="Scenario"/>.
/// Prefer one of the concrete convenience bases (<see cref="MarkdownTest"/>,
/// <see cref="DocumentTest"/>, <see cref="GeneratorTest"/>) over overriding this directly.
///
/// Thread safety: the per-type cache uses <c>ConcurrentDictionary.GetOrAdd</c>;
/// the <see cref="Scenario"/> property may be evaluated more than once if two threads
/// race on the same type, but at most one result is stored and the generator never
/// runs more than once per class (the <see cref="Scenario.BuildAsync"/> returns a
/// <see cref="Task{T}"/> that is <em>not</em> shared across the competing instances — both
/// are independent factory calls — so there is theoretically a race on the first two tests
/// of the first run. In practice, xUnit's per-class sequential execution prevents this).
/// </summary>
public abstract class AuthoringTest
{
	// Keyed by the concrete test class type. Stores the Scenario (which internally owns the
	// Lazy<Task<GeneratorResults>>) so the generator runs exactly once per class.
	private static readonly ConcurrentDictionary<Type, Scenario> ScenarioCache = new();

	/// <summary>
	/// Defines the documentation set for this test class.
	/// Evaluated at most once per class type, the first time <see cref="Docs"/> is accessed.
	/// Deliberately an instance member — a <c>static readonly</c> field initializer would
	/// surface failures as an opaque <see cref="TypeInitializationException"/>.
	/// </summary>
	protected abstract Scenario Scenario { get; }

	/// <summary>
	/// The cached scenario for this test class. All assertion methods are called through this.
	/// </summary>
	protected Scenario Docs => ScenarioCache.GetOrAdd(GetType(), _ => Scenario);
}

/// <summary>
/// Convenience base for tests that pass a Markdown fragment to <c>index.md</c>
/// (wrapped in an H1). Override <see cref="Markdown"/> to define the content.
/// </summary>
public abstract class MarkdownTest : AuthoringTest
{
	[LanguageInjection("markdown")]
	protected abstract string Markdown { get; }

	protected override Scenario Scenario => Setup.Markdown(Markdown);
}

/// <summary>
/// Convenience base for tests that pass a full documentation page as <c>index.md</c>
/// (supplying their own front matter and H1). Override <see cref="Document"/> to define the content.
/// </summary>
public abstract class DocumentTest : AuthoringTest
{
	[LanguageInjection("markdown")]
	protected abstract string Document { get; }

	protected override Scenario Scenario => Setup.Document(Document);
}

/// <summary>
/// Convenience base for tests that assemble a multi-file documentation set.
/// Override <see cref="Files"/> (and optionally <see cref="Options"/>) to define the set.
/// </summary>
public abstract class GeneratorTest : AuthoringTest
{
	protected abstract IReadOnlyCollection<TestFile> Files { get; }
	protected virtual SetupOptions Options => SetupOptions.Empty;

	protected override Scenario Scenario => Setup.Generate(Options, Files);
}

/// <summary>
/// A helper that wraps a <see cref="Task{T}"/> of <see cref="MarkdownResult"/> and exposes
/// the per-file assertion methods, so that chaining reads like the F# pipe operator:
/// <code>
/// await Docs.Converts("folder/relative.md").ContainsHtml("...");
/// </code>
/// </summary>
public sealed class MarkdownResultTask(Task<MarkdownResult> inner)
{
	public Task<MarkdownResult> Inner => inner;

	/// <summary>Asserts the file's HTML contains the expected fragment (pretty-printed).</summary>
	public async Task ContainsHtml([LanguageInjection("html")] string expected) => HtmlAssertions.ContainsHtml(expected, await inner);

	/// <summary>Asserts the file's raw HTML contains the expected string verbatim.</summary>
	public async Task ContainsRawHtml(string expected) => HtmlAssertions.ContainsRawHtml(expected, await inner);

	/// <summary>Asserts the file's raw HTML does NOT contain the expected string.</summary>
	public async Task DoesNotContainHtml(string expected) => HtmlAssertions.DoesNotContainHtml(expected, await inner);

	/// <summary>
	/// Asserts the file's HTML matches the expected fragment exactly (pretty-printed diff).
	/// Mirrors F# <c>toHtml</c>.
	/// </summary>
	public async Task ToHtml([LanguageInjection("html")] string expected) => HtmlAssertions.ToHtml(expected, await inner);

	/// <summary>Finds all <typeparamref name="T"/> instances in the fully-parsed document.</summary>
	public async Task<T[]> Parses<T>() where T : Markdig.Syntax.MarkdownObject => await MarkdownDocumentAssertions.Parses<T>(await inner);

	/// <summary>Finds all <typeparamref name="T"/> instances in the minimally-parsed document.</summary>
	public async Task<T[]> ParsesMinimal<T>() where T : Markdig.Syntax.MarkdownObject =>
		await MarkdownDocumentAssertions.ParsesMinimal<T>(await inner);

	/// <summary>Returns the underlying <see cref="MarkdownFile"/> for this result.</summary>
	public async Task<Elastic.Markdown.IO.MarkdownFile> MarkdownFile() => (await inner).File;
}

/// <summary>
/// Extension methods on <see cref="MarkdownResultTask"/> for applicability assertions.
/// Kept as extensions so they appear alongside the base assertion methods in IntelliSense.
/// </summary>
public static class MarkdownResultTaskExtensions
{
	/// <summary>
	/// Asserts that the first element in the array has the expected <c>AppliesTo</c>.
	/// Mirrors F# <c>appliesToDirective</c>.
	/// </summary>
	public static async Task AppliesToDirective<T>(
		this Task<T[]> directives,
		Elastic.Documentation.AppliesTo.ApplicableTo expected
	) where T : Elastic.Documentation.AppliesTo.IApplicableToElement
	{
		var d = (await directives).FirstOrDefault()
			?? throw new Xunit.Sdk.XunitException("Could not locate an element for AppliesToDirective");
		d.AppliesTo.Should().Be(expected);
	}
}

/// <summary>
/// A static helper that provides the <c>Applies("ga 9.0")</c> shorthand,
/// mirroring F# <c>AppliesCollection.op_Explicit "ga 9.0"</c>.
/// Global-using-static'd via <c>GlobalUsings.cs</c>.
/// </summary>
public static class AppliesHelper
{
	/// <summary>
	/// Parses an applicability spec string. E.g. <c>Applies("ga 9.0")</c>.
	/// Equivalent to F# <c>(AppliesCollection)"ga 9.0"</c>.
	/// </summary>
	public static Elastic.Documentation.AppliesTo.AppliesCollection Applies(string spec) =>
		(Elastic.Documentation.AppliesTo.AppliesCollection)spec;
}
