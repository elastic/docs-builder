// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Extensions;
using Nullean.ScopedFileSystem;
using DirectoryInfoExtensions = Elastic.Documentation.Extensions.IDirectoryInfoExtensions;

// ReSharper disable once CheckNamespace — intentionally preserving the original namespace so consumers need no using changes
#pragma warning disable IDE0130
namespace Elastic.Documentation.Configuration;

public static class FileSystemFactory
{
	// Read options: workspace + app data, all confirmed hidden names allowed.
	// Includes .git (GitCheckoutInformation reads it) and .artifacts/.doc.state
	// (incremental build reads existing output state).
	private static readonly ScopedFileSystemOptions WorkingDirectoryReadOptions = new(
		[Paths.WorkingDirectoryRoot.FullName, Paths.ApplicationData.FullName])
	{
		AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".artifacts" },
		AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".doc.state", ".pagefind-net-frontend-version" }
	};

	// Write options: same scope roots but no .git — nothing in the build output
	// pipeline should ever write into the git repository metadata.
	// Temp is allowed because deploy operations (e.g. S3 sync) stage files there.
	private static readonly ScopedFileSystemOptions WorkingDirectoryWriteOptions = new(
		[Paths.WorkingDirectoryRoot.FullName, Paths.ApplicationData.FullName])
	{
		AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".artifacts" },
		AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".doc.state", ".pagefind-net-frontend-version" },
		AllowedSpecialFolders = AllowedSpecialFolder.Temp
	};

	// AppData-only options: for components that only access caches/state files.
	private static readonly ScopedFileSystemOptions AppDataOptions = new([Paths.ApplicationData.FullName])
	{
		// .git needed for codex-link-index clone directory inside ApplicationData
		AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" }
	};

	/// <summary>
	/// A pre-allocated <see cref="ScopedFileSystem"/> for reading workspace files.
	/// Scoped to the working directory root and per-user app data; allows <c>.git</c>
	/// (read by <c>GitCheckoutInformation</c>), <c>.artifacts</c> and <c>.doc.state</c>
	/// (read for incremental build state).
	/// </summary>
	public static ScopedFileSystem RealRead { get; } = new(new FileSystem(), WorkingDirectoryReadOptions);

	/// <summary>
	/// A pre-allocated <see cref="ScopedFileSystem"/> for writing build output.
	/// Same scope as <see cref="RealRead"/> but without <c>.git</c> access —
	/// nothing in the output pipeline should write into git repository metadata.
	/// </summary>
	public static ScopedFileSystem RealWrite { get; } = new(new FileSystem(), WorkingDirectoryWriteOptions);

	/// <summary>
	/// A pre-allocated <see cref="ScopedFileSystem"/> scoped only to the per-user
	/// <c>elastic/docs-builder</c> application data folder. Use for components that
	/// access caches or state and have no need for workspace files
	/// (e.g. <c>CrossLinkFetcher</c>, <c>CheckForUpdatesFilter</c>, <c>GitLinkIndexReader</c>).
	/// </summary>
	public static ScopedFileSystem AppData { get; } = new(new FileSystem(), AppDataOptions);

	/// <summary>
	/// Scopes <paramref name="inner"/> to <see cref="Paths.WorkingDirectoryRoot"/> and
	/// <see cref="Paths.ApplicationData"/> for reading. Use when the inner FS contains files
	/// that live within the current working-directory tree (e.g. a test <c>MockFileSystem</c>
	/// seeded with workspace-relative paths).
	/// </summary>
	public static ScopedFileSystem ScopeCurrentWorkingDirectory(IFileSystem inner) =>
		new(inner, WorkingDirectoryReadOptions);

	/// <summary>
	/// Scopes <paramref name="inner"/> to <see cref="Paths.WorkingDirectoryRoot"/> and
	/// <see cref="Paths.ApplicationData"/> for reading, extended by <paramref name="extensionRoots"/>
	/// (e.g. detection-rules folders declared via
	/// <see cref="IDocsBuilderExtension.ExternalScopeRoots"/>).
	/// </summary>
	public static ScopedFileSystem ScopeCurrentWorkingDirectory(IFileSystem inner, IEnumerable<string>? extensionRoots)
	{
		if (extensionRoots is null)
			return ScopeCurrentWorkingDirectory(inner);

		var workingRoot = inner.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName);
		var externalRoots = extensionRoots
			.Select(r => inner.DirectoryInfo.New(r))
			// Drop descendants of the working root (already covered by the base scope).
			// Also drop ancestors of the working root — they would subsume the working root,
			// producing overlapping roots that ScopedFileSystem rejects with ArgumentException.
			.Where(d => !DirectoryInfoExtensions.IsSubPathOf(d, workingRoot) && !DirectoryInfoExtensions.IsSubPathOf(workingRoot, d))
			.Select(d => d.FullName)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		if (externalRoots.Length == 0)
			return ScopeCurrentWorkingDirectory(inner);

		var roots = new[] { Paths.WorkingDirectoryRoot.FullName, Paths.ApplicationData.FullName }
			.Concat(externalRoots)
			.ToArray();

		return new ScopedFileSystem(inner, new ScopedFileSystemOptions(roots)
		{
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".artifacts" },
			AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".doc.state", ".pagefind-net-frontend-version" }
		});
	}

	// Builds write options that include AllowedSpecialFolders.Temp PLUS the inner FS's own
	// GetTempPath() as an explicit root — but only when the inner FS is MockFileSystem.
	//
	// On non-Windows MockFileSystem hardcodes a Unix-ified path ("/temp/", derived from "C:\temp")
	// instead of calling System.IO.Path.GetTempPath(). AllowedSpecialFolder.Temp uses the real
	// GetTempPath() (e.g. "/tmp/" on Linux), so the two diverge and scope validation fails for any
	// path created via mockFs.Path.GetTempPath().
	//
	// Fix tracked upstream: https://github.com/TestableIO/System.IO.Abstractions/pull/1454
	// Once that ships and we update the package reference we can drop this workaround.
	//
	// We use ScopedFileSystem.InnerType (added in Nullean.ScopedFileSystem 0.4.0) to avoid a
	// fragile string-based type check.
	private static ScopedFileSystemOptions BuildWriteOptions(IFileSystem inner, params string[] roots)
	{
		var allRoots = roots.ToList();
		var innerType = inner is ScopedFileSystem sf ? sf.InnerType : inner.GetType();
		if (!OperatingSystem.IsWindows() && innerType.Name.Contains("Mock", StringComparison.OrdinalIgnoreCase))
		{
			// Cover MockFileSystem's unixified hardcoded temp path
			var innerTemp = inner.Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (!string.IsNullOrEmpty(innerTemp) && !allRoots.Contains(innerTemp, StringComparer.OrdinalIgnoreCase))
				allRoots.Add(innerTemp);
		}
		return new ScopedFileSystemOptions([.. allRoots])
		{
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".artifacts" },
			AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".doc.state", ".pagefind-net-frontend-version" },
			AllowedSpecialFolders = AllowedSpecialFolder.Temp
		};
	}

	/// <summary>
	/// Scopes <paramref name="inner"/> to <see cref="Paths.WorkingDirectoryRoot"/> and
	/// <see cref="Paths.ApplicationData"/> for writing (.git not allowed). Use when
	/// the inner FS writes into the working-directory tree.
	/// </summary>
	public static ScopedFileSystem ScopeCurrentWorkingDirectoryForWrite(IFileSystem inner) =>
		new(inner, BuildWriteOptions(
			inner, Paths.WorkingDirectoryRoot.FullName, Paths.ApplicationData.FullName));

	/// <summary>
	/// Scopes <paramref name="inner"/> to an explicit <paramref name="sourceRoot"/> and
	/// <see cref="Paths.ApplicationData"/> for reading. Use when the files to be read live under
	/// a specific known root that is not <see cref="Paths.WorkingDirectoryRoot"/> — for example
	/// test fixtures with assembler-checkout paths or service code operating on a given directory.
	/// </summary>
	public static ScopedFileSystem ScopeSourceDirectory(IFileSystem inner, string sourceRoot) =>
		new(inner, new ScopedFileSystemOptions([sourceRoot, Paths.ApplicationData.FullName])
		{
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".artifacts" },
			AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".doc.state", ".pagefind-net-frontend-version" }
		});

	/// <summary>
	/// Creates a read <see cref="ScopedFileSystem"/> scoped to the git root of
	/// <paramref name="path"/>. Falls back to <see cref="RealRead"/> when <paramref name="path"/>
	/// is <see langword="null"/>. Use in commands that accept an explicit <c>--path</c> argument.
	/// <para>
	/// Suitable for command-layer code. Service-layer tests use <see cref="InMemory()"/> directly
	/// and do not exercise this method.
	/// </para>
	/// </summary>
	public static ScopedFileSystem RealGitRootForPath(string? path)
	{
		var plain = new FileSystem();
		string root;
		if (path is null)
			root = Paths.WorkingDirectoryRoot.FullName;
		else
		{
			var startDir = plain.DirectoryInfo.New(
				plain.Directory.Exists(path) ? path : plain.Path.GetDirectoryName(path) ?? path);
			root = Paths.FindGitRoot(startDir)?.FullName ?? startDir.FullName;
		}

		var roots = new List<string> { root, Paths.ApplicationData.FullName };

		// In a git worktree the local .git entry is a file pointing to the main repo's git dir.
		// Resolve it via TryReadGitDirPointer (handles relative paths and commondir) and add the
		// real .git directory to the scope so config/HEAD reads are not rejected.
		var gitFilePath = plain.Path.Join(root, ".git");
		if (plain.File.Exists(gitFilePath)
			&& Paths.TryReadGitDirPointer(plain, plain.FileInfo.New(gitFilePath), out var resolvedGitDir)
			&& resolvedGitDir is not null)
		{
			roots.Add(resolvedGitDir.FullName);
		}

		// Fast path: no worktree detected and path was null — reuse the pre-built instance
		if (roots.Count == 2 && path is null)
			return RealRead;

		return new ScopedFileSystem(plain, new ScopedFileSystemOptions([.. roots])
		{
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".artifacts" },
			AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".doc.state", ".pagefind-net-frontend-version" }
		});
	}

	/// <summary>
	/// Creates a write <see cref="ScopedFileSystem"/> scoped to the git root of
	/// <paramref name="path"/> (and <paramref name="output"/> if it falls outside that root).
	/// Falls back to <see cref="RealWrite"/> when both are <see langword="null"/>.
	/// Use in commands that accept explicit <c>--path</c> and/or <c>--output</c> arguments.
	/// </summary>
	public static ScopedFileSystem RealGitRootForPathWrite(string? path, string? output = null)
	{
		if (path is null && output is null)
			return RealWrite;

		var plain = new FileSystem();
		string gitRoot;
		if (path is not null)
		{
			var startDir = plain.DirectoryInfo.New(
				plain.Directory.Exists(path) ? path : plain.Path.GetDirectoryName(path) ?? path);
			gitRoot = Paths.FindGitRoot(startDir)?.FullName ?? startDir.FullName;
		}
		else
			gitRoot = Paths.WorkingDirectoryRoot.FullName;

		var roots = new List<string> { gitRoot, Paths.ApplicationData.FullName };

		if (output is not null)
		{
			var absOutput = Path.IsPathRooted(output) ? output : Path.GetFullPath(output);
			if (!DirectoryInfoExtensions.IsSubPathOf(plain.DirectoryInfo.New(absOutput), plain.DirectoryInfo.New(gitRoot)))
				roots.Add(absOutput);
		}

		return new ScopedFileSystem(plain, BuildWriteOptions(plain, [.. roots]));
	}

	/// <summary>
	/// Creates a read <see cref="ScopedFileSystem"/> for CI environments,
	/// extending the default scope with <c>RUNNER_TEMP</c> when available.
	/// Falls back to <see cref="RealRead"/> when <c>RUNNER_TEMP</c> is not set.
	/// Use in CI commands that need to read temporary files staged in the GitHub Actions runner.
	/// </summary>
	public static ScopedFileSystem RealReadForRunnerTemp(IEnvironmentVariables? environmentVariables = null)
	{
		var runnerTemp = environmentVariables?.GetEnvironmentVariable("RUNNER_TEMP");
		if (string.IsNullOrWhiteSpace(runnerTemp))
			return RealRead;

		return ScopeCurrentWorkingDirectory(new FileSystem(), [runnerTemp]);
	}
}
