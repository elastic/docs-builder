// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using DiffEngine;

namespace Elastic.ApiExplorer.Tests;

public static class VerifyInitializer
{
	[ModuleInitializer]
	public static void Initialize()
	{
		// Pretty-prints and compares HTML structurally, so attribute/whitespace formatting never shows up as a diff.
		VerifyAngleSharpDiffing.Initialize();
		VerifierSettings.ScrubInlineGuids();
		// Never open a diff tool on CI; a mismatch must fail the run, and Verify never auto-accepts.
		if (BuildServerDetector.Detected)
			DiffRunner.Disabled = true;
	}
}
