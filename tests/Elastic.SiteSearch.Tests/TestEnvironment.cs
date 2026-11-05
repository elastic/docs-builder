// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;

namespace Elastic.SiteSearch.Tests;

internal static class TestEnvironment
{
	// ContentStackMapper.ParseDate reads a date without an offset ("2023-09-01") in the machine's time zone, so the mapped
	// published_date of a press or product release carries the offset of whoever ran the test: +01:00 on a laptop in
	// Amsterdam, +00:00 on a GitHub runner. Pin the zone so the verified files read the same everywhere. TZ is read by
	// .NET on Linux and macOS; Windows ignores it, but its CI runners are on UTC.
#pragma warning disable CA2255 // a test assembly is application code
	[ModuleInitializer]
#pragma warning restore CA2255
	internal static void PinTimeZone() => Environment.SetEnvironmentVariable("TZ", "UTC");
}
