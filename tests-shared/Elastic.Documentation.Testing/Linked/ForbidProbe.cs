// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Nullean.Snapshots;

namespace Elastic.Documentation.Testing.Linked;

/// <summary>
/// Shows that a forbidden value stops a snapshot. The text is its own inline snapshot, so it matches whenever the forbid check does
/// not run: only that check can make the call throw, and a missing snapshot cannot stand in for it.
/// </summary>
internal static class ForbidProbe
{
	/// <summary>The message of the failure, or <c>null</c> when the snapshot passed, and how many received files hold <paramref name="value"/>.</summary>
	internal static (string? Message, int ReceivedFilesWithValue) Run(string text, string value, SnapshotOptions? options = null)
	{
		string? message = null;
		try
		{
			text.Should().MatchInlineSnapshot(text, options);
		}
		catch (Exception e)
		{
			message = e.Message;
		}

		return (message, ReceivedFilesWithValue(value));
	}

	private static int ReceivedFilesWithValue(string value)
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docs-builder.slnx")))
			dir = dir.Parent;
		var received = dir is null ? null : Path.Join(dir.FullName, ".artifacts", ".snapshots", "received");
		return received is null || !Directory.Exists(received)
			? 0
			: Directory.EnumerateFiles(received, "*", SearchOption.AllDirectories).Count(
				f => File.ReadAllText(f).Contains(value, StringComparison.Ordinal)
			);
	}
}
