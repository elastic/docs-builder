// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using Elastic.Documentation.FileSystems;
using System.IO.Abstractions;
using System.Security;
using System.Text;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Evaluation;

public static class ChangelogPrBodyReader
{
	// PR_BODY can hit GitHub's 65,536-char limit and exceed runner env-var
	// budgets when passed inline. PR_BODY_FILE lets callers stage the body
	// in a file under RUNNER_TEMP and pass the path instead, which keeps
	// the body off the env block entirely. Cap reads at 256 KiB to bound
	// memory if a caller hands us a hostile path.
	internal const int MaxPrBodyFileBytes = 256 * 1024;

	public static async Task<string?> ReadAsync(
		string? prBodyFile,
		IDiagnosticsCollector collector,
		IRunnerTempFileSystem fileSystem,
		CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(prBodyFile))
			return null;

		try
		{
			var info = fileSystem.FileInfo.New(prBodyFile);
			if (!info.Exists)
			{
				collector.EmitWarning(string.Empty, $"PR_BODY_FILE points to a missing file: {prBodyFile}");
				return null;
			}

			if (info.Length <= MaxPrBodyFileBytes)
				return await fileSystem.File.ReadAllTextAsync(prBodyFile, ct);

			collector.EmitHint(string.Empty, $"PR_BODY_FILE exceeds {MaxPrBodyFileBytes} bytes ({info.Length}); truncating.");

			var buffer = ArrayPool<byte>.Shared.Rent(MaxPrBodyFileBytes);
			try
			{
				await using var stream = info.OpenRead();
				var slice = buffer.AsMemory(0, MaxPrBodyFileBytes);
				await stream.ReadExactlyAsync(slice, ct);
				return Encoding.UTF8.GetString(slice.Span);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}
		catch (SecurityException ex)
		{
			collector.EmitWarning(string.Empty, $"PR_BODY_FILE is not readable: {prBodyFile} ({ex.Message})");
			return null;
		}
	}
}
