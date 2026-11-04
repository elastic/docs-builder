// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>
/// The pivots, beyond the always-required type, that <c>pivot.required</c> makes mandatory:
/// a PR must carry a label that maps to each of them.
/// </summary>
[Flags]
public enum RequiredPivot
{
	None = 0,
	Areas = 1,
	Products = 2
}
