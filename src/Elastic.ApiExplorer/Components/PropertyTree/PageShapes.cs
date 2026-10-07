// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Components.PropertyTree;

/// <summary>
/// The structures one page has already listed, so a repeat links back instead of listing them again. Specs repeat large
/// shapes: every rule type carries the same <c>actions</c>, and a result lists the same rule union for created, updated and
/// deleted. One builder serves one page, so the registry never spans pages.
/// </summary>
/// <remarks>
/// A shape key (a <c>$ref</c> or a union's option fingerprint) only finds candidates. A repeat shares a listing only when
/// its built content hashes the same, so two unions that differ in an enum value or a deep field never merge.
/// </remarks>
public sealed class PageShapes
{
	// Smaller structures read better inline than behind a link.
	private const int MinRowsToShare = 10;

	private readonly Dictionary<string, RepeatedShape> _expanding = [];
	private readonly Dictionary<string, List<(RepeatedShape Shape, string Content)>> _listed = [];
	private readonly Dictionary<RepeatedShape, string> _contentOf = [with(ReferenceEqualityComparer.Instance)];
	private readonly List<(string Key, RepeatedShape Shape)> _recorded = [];
	private readonly Dictionary<RepeatedShape, string> _keyOf = [with(ReferenceEqualityComparer.Instance)];

	/// <summary>The enclosing row a key is being expanded for; repeating it is a recursion, which always links.</summary>
	public RepeatedShape? Ancestor(string? key) => key is not null && _expanding.TryGetValue(key, out var ancestor) ? ancestor : null;

	public void BeginListing(string key, RepeatedShape shape)
	{
		_ = _expanding.TryAdd(key, shape);
		_keyOf[shape] = key;
	}

	/// <summary>The shape key a listing was started for; a recursion back to it hashes by this, not by any row name.</summary>
	public string? KeyOf(RepeatedShape shape) => _keyOf.GetValueOrDefault(shape);

	public void EndListing(string key) => _ = _expanding.Remove(key);

	/// <summary>An earlier listing with exactly this content, if any.</summary>
	public RepeatedShape? Listing(string key, string content) =>
		_listed.TryGetValue(key, out var listings) ? listings.FirstOrDefault(l => l.Content == content).Shape : null;

	/// <summary>The content hash of a shared listing, so a repeat counts the same as the listing it points to.</summary>
	public string? ContentOf(RepeatedShape shape) => _contentOf.GetValueOrDefault(shape);

	public void Record(string key, RepeatedShape shape, string content, int rowCount)
	{
		if (rowCount < MinRowsToShare)
			return;
		if (!_listed.TryGetValue(key, out var listings))
			_listed[key] = listings = [];
		listings.Add((shape, content));
		_contentOf[shape] = content;
		_recorded.Add((key, shape));
	}

	/// <summary>A point to roll back to when a built copy turns out to repeat an earlier listing and is dropped.</summary>
	public int Checkpoint => _recorded.Count;

	/// <summary>Forgets the listings recorded inside a dropped copy, so no link points into content that is not on the page.</summary>
	public void Rollback(int checkpoint)
	{
		for (var i = _recorded.Count - 1; i >= checkpoint; i--)
		{
			var (key, shape) = _recorded[i];
			var listings = _listed[key];
			listings.RemoveAt(listings.Count - 1);
			_ = _contentOf.Remove(shape);
		}

		_recorded.RemoveRange(checkpoint, _recorded.Count - checkpoint);
	}
}
