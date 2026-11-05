// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests;

internal static class SnapshotSetup
{
	// Most tests work in a folder named after a fresh Guid, and its path shows up in file paths in the output.
	// Number the Guids (Guid_1, Guid_2, ...) so that the same folder reads the same everywhere in one snapshot.
	[ModuleInitializer]
	internal static void ScrubGuids() => SnapshotDefaults.AddScrubber(Scrubbers.Guids);
}
