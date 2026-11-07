// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation;

/// <summary>
/// Shared regular expression match timeout. Every regex must use it to bound the cost of
/// pathological input (ReDoS). It is a <c>const</c> so it can be used in <c>[GeneratedRegex]</c> attributes.
/// </summary>
public static class RegexTimeouts
{
	/// <summary>Match timeout in milliseconds applied to every regex (10 seconds).</summary>
	public const int MatchTimeoutMilliseconds = 10_000;

	/// <summary>The same timeout as a <see cref="TimeSpan"/>, for <c>new Regex(...)</c> and static <c>Regex</c> calls.</summary>
	public static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(MatchTimeoutMilliseconds);
}
