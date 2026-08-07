// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// Write scope for a documentation set or checkout tree. Sibling of <c>DocumentationFileSystem</c>
/// under <c>ScopedFileSystem</c> — it does <strong>not</strong> derive from
/// <c>DocumentationFileSystem</c> so that write slots typed to this class cannot silently accept
/// the read aggregate, and vice versa.
/// <para>
/// The write scope intentionally omits <c>.git</c> from <see cref="ScopedFileSystemOptions.AllowedHiddenFolderNames"/>:
/// nothing in the build output pipeline should ever write into git repository metadata.
/// </para>
/// </summary>
/// <summary>
/// Constructs the write scope for a documentation set.
/// </summary>
/// <param name="checkout">The repository checkout root (the directory containing <c>.git</c>).</param>
/// <param name="output">
/// Optional explicit output directory. When it falls outside <paramref name="checkout"/> (e.g.
/// <c>--output /tmp/build</c>), it is added as a second scope root. When <see langword="null"/>,
/// output is assumed to be under <paramref name="checkout"/>/.artifacts and is therefore already covered.
/// </param>
/// <param name="inner">
/// The underlying filesystem. Defaults to a new <see cref="FileSystem"/> when <see langword="null"/>.
/// Pass a mock in tests.
/// </param>
public class DocumentationWriteFileSystem(
	IDirectoryInfo checkout,
	IDirectoryInfo? output = null,
	IFileSystem? inner = null)
	: ScopedFileSystem(inner ?? new FileSystem(), BuildOptions(checkout.FullName, output?.FullName, inner))
{

	/// <summary>
	/// The per-user application data directory for <c>elastic/docs-builder</c>.
	/// Same value as <c>Paths.ApplicationData.FullName</c> from the Tooling project, computed here to
	/// avoid a circular project reference.
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
		string checkoutPath,
		string? outputPath,
		IFileSystem? inner)
	{
		var roots = new List<string> { checkoutPath, ApplicationDataPath };

		if (outputPath is not null)
		{
			var sep = System.IO.Path.DirectorySeparatorChar;
			var outputNorm = outputPath.TrimEnd(sep) + sep;
			var checkoutNorm = checkoutPath.TrimEnd(sep) + sep;
			if (!outputNorm.StartsWith(checkoutNorm, StringComparison.OrdinalIgnoreCase))
				roots.Add(outputPath);
		}

		// On non-Windows, MockFileSystem hardcodes a Unix-ified path ("/temp/", derived from "C:\temp")
		// instead of calling System.IO.Path.GetTempPath(). AllowedSpecialFolder.Temp uses the real
		// GetTempPath() (e.g. "/tmp/" on Linux), so the two diverge and scope validation fails for any
		// path created via mockFs.Path.GetTempPath().
		//
		// Fix tracked upstream: https://github.com/TestableIO/System.IO.Abstractions/pull/1454
		// Once that ships and we update the package reference we can drop this workaround.
		var innerResolved = inner ?? new FileSystem();
		var innerType = innerResolved is ScopedFileSystem sf ? sf.InnerType : innerResolved.GetType();
		if (!OperatingSystem.IsWindows() && innerType.Name.Contains("Mock", StringComparison.OrdinalIgnoreCase))
		{
			var innerTemp = innerResolved.Path.GetTempPath().TrimEnd(
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
