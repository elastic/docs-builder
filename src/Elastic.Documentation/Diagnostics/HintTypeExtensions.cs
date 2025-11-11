// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Diagnostics;

/// <summary>
/// Extension methods for working with HintType suppressions.
/// </summary>
public static class HintTypeExtensions
{
	/// <summary>
	/// Checks if a specific hint type should be suppressed.
	/// </summary>
	/// <param name="suppressions">The set of suppressed hint types.</param>
	/// <param name="hintType">The hint type to check.</param>
	/// <returns>True if the hint should be suppressed, false otherwise.</returns>
	public static bool ShouldSuppress(this HashSet<HintType>? suppressions, HintType hintType) =>
		suppressions?.Contains(hintType) == true;
}
