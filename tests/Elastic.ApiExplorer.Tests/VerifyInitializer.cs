// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DiffEngine;

namespace Elastic.ApiExplorer.Tests;

public static partial class VerifyInitializer
{
	[ModuleInitializer]
	public static void Initialize()
	{
		// Pretty-prints and compares HTML structurally, so attribute/whitespace formatting never shows up as a diff.
		VerifyAngleSharpDiffing.Initialize();
		VerifierSettings.ScrubInlineGuids();
		// Static asset URLs carry a content hash of the built frontend, and the footer carries the current year.
		VerifierSettings.AddScrubber(sb =>
		{
			var scrubbed = AssetHash().Replace(sb.ToString(), "?v=ASSET_HASH");
			scrubbed = CopyrightYear().Replace(scrubbed, "© YEAR");
			sb.Clear().Append(scrubbed);
		});
		// Never open a diff tool on CI; a mismatch must fail the run, and Verify never auto-accepts.
		if (BuildServerDetector.Detected)
			DiffRunner.Disabled = true;
	}

	[GeneratedRegex("\\?v=[0-9a-f]{16}")]
	private static partial Regex AssetHash();

	[GeneratedRegex("© \\d{4}")]
	private static partial Regex CopyrightYear();
}
