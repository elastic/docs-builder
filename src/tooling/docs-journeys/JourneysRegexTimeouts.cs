// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Journeys.Cli;

// This tool does not reference Elastic.Documentation, so it cannot use its RegexTimeouts. Keep the value in step with it.
internal static class JourneysRegexTimeouts
{
	public const int MatchTimeoutMilliseconds = 10_000;
}
