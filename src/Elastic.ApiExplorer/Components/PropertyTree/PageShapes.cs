// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Components.PropertyTree;

/// <summary>
/// The structures one page has already listed, so a repeat links back instead of listing them again. Specs repeat large
/// shapes: every rule type carries the same <c>actions</c>, and a result lists the same rule union for created, updated and
/// deleted. One builder serves one page, so the registry never spans pages.
/// </summary>
public sealed class PageShapes
{
	// Smaller structures read better inline than behind a link.
	private const int MinRowsToShare = 10;

	private readonly Dictionary<string, RepeatedShape> _expanding = [];
	private readonly Dictionary<string, RepeatedShape> _listed = [];

	/// <summary>
	/// The earlier listing a row repeats, if any. A shape still being expanded is an ancestor, so repeating it is a recursion
	/// and always links, whatever its size.
	/// </summary>
	public (RepeatedShape? Shape, bool IsAncestor) Find(string? key)
	{
		if (key is null)
			return (null, false);
		if (_expanding.TryGetValue(key, out var ancestor))
			return (ancestor, true);
		return _listed.TryGetValue(key, out var listed) ? (listed, false) : (null, false);
	}

	public void BeginListing(string key, RepeatedShape shape) => _ = _expanding.TryAdd(key, shape);

	public void EndListing(string key, RepeatedShape shape, int rowCount)
	{
		_ = _expanding.Remove(key);
		if (rowCount >= MinRowsToShare)
			_ = _listed.TryAdd(key, shape);
	}
}
