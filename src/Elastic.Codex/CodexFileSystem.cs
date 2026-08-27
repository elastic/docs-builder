// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.FileSystems;

namespace Elastic.Codex;

/// <summary>
/// Scope over the working directory for codex operations. Rooted at the process working directory
/// with the config file's git root added as an extra allowed root, so the config file and any
/// paths inside its repository are always readable.
/// <para>
/// Use this in codex commands instead of constructing a <see cref="CheckoutsFileSystem"/> manually.
/// </para>
/// </summary>
public class CodexFileSystem : CheckoutsFileSystem
{
	private static readonly FileSystem Physical = new();

	/// <summary>The codex configuration file, resolved through this scoped filesystem.</summary>
	public IFileInfo ConfigurationFile { get; }

	/// <param name="config">Full path to the codex configuration file. Its directory is used to locate the git root.</param>
	/// <param name="output">Optional explicit output directory.</param>
	/// <param name="inner">Underlying filesystem — defaults to the physical filesystem when <see langword="null"/>.</param>
	public CodexFileSystem(string config, string? output = null, IFileSystem? inner = null) : this(inner ?? Physical, config, output, inner) { }

	private CodexFileSystem(IFileSystem fs, string config, string? output, IFileSystem? inner) : base(
			root: fs.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName),
			output: output is not null ? fs.DirectoryInfo.New(output) : null,
			inner: inner,
			extraRoots: [
				Paths.FindGitRoot(fs.DirectoryInfo.New(fs.Path.GetDirectoryName(config)!))?.FullName ?? fs.Path.GetDirectoryName(config)!
			]
		) => ConfigurationFile = FileInfo.New(config);
}
