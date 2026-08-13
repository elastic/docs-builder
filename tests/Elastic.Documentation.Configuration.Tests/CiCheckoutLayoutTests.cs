// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.FileSystems;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// Regression tests for the GitHub Actions hosted-runner layout where each documentation
/// set checkout lives <em>inside</em> the ApplicationData folder:
/// <c>~/.local/share/elastic/docs-builder/checkouts/current/&lt;repo&gt;</c>
/// <para>
/// Before the fix, all three filesystem types unconditionally added ApplicationData as a
/// second scope root alongside the checkout. When checkout ⊂ AppData, the two roots form
/// a parent–child pair and <c>ValidateRootsAreDisjoint</c> throws with "Scope roots must
/// be disjoint". Now each type skips AppData when checkout is a sub-path of it (or vice
/// versa).
/// </para>
/// </summary>
public class CiCheckoutLayoutTests
{
	/// <summary>
	/// Constructs the simulated CI checkout path: a directory nested inside the real
	/// ApplicationData folder, matching the layout the assembler uses on hosted runners.
	/// </summary>
	private static string CiCheckoutRoot =>
		Path.Join(Paths.ApplicationData.FullName, "checkouts", "current", "apm-server");

	// -----------------------------------------------------------------------
	// CheckoutsFileSystem
	// -----------------------------------------------------------------------

	[Fact]
	public void CheckoutsFileSystem_CheckoutInsideAppData_DoesNotThrow()
	{
		var checkoutRoot = CiCheckoutRoot;
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ Path.Join(checkoutRoot, "readme.md"), new MockFileData("hello") }
		});

		var act = () => new CheckoutsFileSystem(mockFs.DirectoryInfo.New(checkoutRoot), inner: mockFs);

		act.Should().NotThrow();
	}

	[Fact]
	public void CheckoutsFileSystem_CheckoutInsideAppData_ReadsFilesUnderCheckout()
	{
		var checkoutRoot = CiCheckoutRoot;
		var filePath = Path.Join(checkoutRoot, "readme.md");
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ filePath, new MockFileData("hello") }
		});

		var fs = new CheckoutsFileSystem(mockFs.DirectoryInfo.New(checkoutRoot), inner: mockFs);

		fs.File.Exists(filePath).Should().BeTrue();
	}

	// -----------------------------------------------------------------------
	// DocumentationWriteFileSystem
	// -----------------------------------------------------------------------

	[Fact]
	public void DocumentationWriteFileSystem_CheckoutInsideAppData_DoesNotThrow()
	{
		var checkoutRoot = CiCheckoutRoot;
		var mockFs = new MockFileSystem();

		var act = () => new DocumentationWriteFileSystem(
			mockFs.DirectoryInfo.New(checkoutRoot),
			inner: mockFs);

		act.Should().NotThrow();
	}

	[Fact]
	public void DocumentationWriteFileSystem_CheckoutInsideAppData_WritesFilesUnderCheckout()
	{
		var checkoutRoot = CiCheckoutRoot;
		var outputPath = Path.Join(checkoutRoot, ".artifacts", "docs", "html");
		var mockFs = new MockFileSystem();

		var writeFs = new DocumentationWriteFileSystem(
			mockFs.DirectoryInfo.New(checkoutRoot),
			inner: mockFs);

		var act = () => writeFs.Directory.CreateDirectory(outputPath);
		act.Should().NotThrow();
	}

	// -----------------------------------------------------------------------
	// DocumentationFileSystem (read + write via Resolve)
	// -----------------------------------------------------------------------

	[Fact]
	public void DocumentationFileSystem_Resolve_CheckoutInsideAppData_DoesNotThrow()
	{
		var checkoutRoot = CiCheckoutRoot;
		var docsPath = Path.Join(checkoutRoot, "docs");
		var mockFs = BuildDocsetFs(checkoutRoot, docsPath);

		var act = () => DocumentationFileSystem.Resolve(
			mockFs.DirectoryInfo.New(docsPath),
			new DocumentationScopeOptions { Inner = mockFs });

		act.Should().NotThrow();
	}

	[Fact]
	public void DocumentationFileSystem_Resolve_CheckoutInsideAppData_CheckoutResolvedCorrectly()
	{
		var checkoutRoot = CiCheckoutRoot;
		var docsPath = Path.Join(checkoutRoot, "docs");
		var mockFs = BuildDocsetFs(checkoutRoot, docsPath);

		var docFs = DocumentationFileSystem.Resolve(
			mockFs.DirectoryInfo.New(docsPath),
			new DocumentationScopeOptions { Inner = mockFs });

		docFs.Paths.CheckoutDirectory.FullName.Should().Be(checkoutRoot);
	}

	[Fact]
	public void DocumentationFileSystem_Resolve_CheckoutInsideAppData_WriteDoesNotThrow()
	{
		var checkoutRoot = CiCheckoutRoot;
		var docsPath = Path.Join(checkoutRoot, "docs");
		var mockFs = BuildDocsetFs(checkoutRoot, docsPath);

		var act = () =>
		{
			var docFs = DocumentationFileSystem.Resolve(
				mockFs.DirectoryInfo.New(docsPath),
				new DocumentationScopeOptions { Inner = mockFs });
			// accessing .Write must not throw either
			_ = docFs.Write;
		};

		act.Should().NotThrow();
	}

	// -----------------------------------------------------------------------
	// Helpers
	// -----------------------------------------------------------------------

	private static MockFileSystem BuildDocsetFs(string checkoutRoot, string docsPath)
	{
		var mockFs = new MockFileSystem();
		mockFs.AddDirectory(Path.Join(checkoutRoot, ".git"));
		mockFs.AddFile(Path.Join(checkoutRoot, ".git", "HEAD"),
			new MockFileData("ref: refs/heads/main\n"));
		mockFs.AddFile(Path.Join(checkoutRoot, ".git", "refs", "heads", "main"),
			new MockFileData("abc1234\n"));
		mockFs.AddFile(Path.Join(checkoutRoot, ".git", "config"),
			new MockFileData("""
				[remote "origin"]
					url = https://github.com/elastic/apm-server.git
				[branch "main"]
					remote = origin
					merge = refs/heads/main
				"""));
		mockFs.AddFile(Path.Join(docsPath, "docset.yml"), new MockFileData("toc: []\n"));
		return mockFs;
	}
}
