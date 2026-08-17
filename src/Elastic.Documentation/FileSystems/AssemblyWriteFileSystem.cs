// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Extensions;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// Write scope for the assembler and codex pipeline. Sibling of <c>DocumentationWriteFileSystem</c>
/// under <c>ScopedFileSystem</c> — it does <strong>not</strong> derive from
/// <c>DocumentationWriteFileSystem</c> so that write slots typed to one cannot silently accept the other.
/// <para>
/// Roots: the checkouts directory and the output directory (if they are not already nested inside each
/// other), plus <see cref="AllowedSpecialFolder.Temp"/> for S3 upload staging.
/// </para>
/// </summary>
/// <param name="checkout">The checkouts root (e.g. <c>AppData/checkouts/&lt;source&gt;</c>).</param>
/// <param name="output">
/// Optional explicit output directory. When it falls outside <paramref name="checkout"/>, it is added
/// as a second scope root; when <see langword="null"/>, output is assumed to be under
/// <paramref name="checkout"/>/.artifacts and is therefore already covered.
/// </param>
/// <param name="inner">
/// The underlying filesystem. Defaults to a new <see cref="FileSystem"/> when <see langword="null"/>.
/// Pass a mock in tests.
/// </param>
public class AssemblyWriteFileSystem(
	IDirectoryInfo checkout,
	IDirectoryInfo? output = null,
	IFileSystem? inner = null)
	: ScopedFileSystem(inner ?? new FileSystem(), BuildOptions(checkout, output, inner))
{
	/// <summary>
	/// The per-user application data directory for <c>elastic/docs-builder</c>.
	/// Computed inline (rather than via <c>Paths</c>) to avoid a circular project reference.
	/// </summary>
	private static string ApplicationDataPath
	{
		get
		{
			var localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			if (string.IsNullOrEmpty(localPath))
				localPath = System.IO.Path.GetTempPath();
			return System.IO.Path.Join(localPath, "elastic", "docs-builder");
		}
	}

	private static ScopedFileSystemOptions BuildOptions(
		IDirectoryInfo checkout,
		IDirectoryInfo? output,
		IFileSystem? inner)
	{
		var fs = inner ?? checkout.FileSystem;
		var checkoutPath = checkout.FullName;
		var roots = new List<string> { checkoutPath };

		// On CI the checkouts directory lives inside AppData
		// (/home/runner/.local/share/elastic/docs-builder/checkouts/...). AddDisjointRoot keeps
		// whichever of the two is the outer path rather than dropping AppData outright, so sibling
		// AppData directories stay in scope even when the checkout itself is nested inside AppData.
		roots.AddDisjointRoot(ApplicationDataPath, fs);

		if (output is not null)
			roots.AddDisjointRoot(output.FullName, fs);

		// MockFileSystem hardcodes its temp path ("C:\temp" on Windows, unix-ified to "/temp/"
		// elsewhere) instead of calling System.IO.Path.GetTempPath(). AllowedSpecialFolder.Temp uses
		// the real GetTempPath() (e.g. "/tmp/" on Linux, "C:\Users\<user>\AppData\Local\Temp" on
		// Windows), so the two diverge on every OS and scope validation fails for any path created
		// via mockFs.Path.GetTempPath().
		//
		// Fix tracked upstream: https://github.com/TestableIO/System.IO.Abstractions/pull/1454
		// Once that ships and we update the package reference we can drop this workaround.
		var innerType = fs is ScopedFileSystem sf ? sf.InnerType : fs.GetType();
		if (innerType.Name.Contains("Mock", StringComparison.OrdinalIgnoreCase))
		{
			var innerTemp = fs.Path.GetTempPath().TrimEnd(
				System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
			if (!string.IsNullOrEmpty(innerTemp) && !roots.Contains(innerTemp, StringComparer.OrdinalIgnoreCase))
				roots.Add(innerTemp);
		}

		return new ScopedFileSystemOptions([.. roots])
		{
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".artifacts" },
			AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".doc.state", ".pagefind-net-frontend-version" },
			AllowedSpecialFolders = AllowedSpecialFolder.Temp
		};
	}
}
