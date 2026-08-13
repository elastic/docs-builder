// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Documentation.Navigation;

namespace Elastic.Documentation.Site.Navigation;

/// <summary>
/// Renders each navigation root at most once per build and shares the result with every page under it.
/// Keyed on root identity rather than <see cref="INavigationItem.Id"/> because ids are not unique across
/// roots. Concurrent callers for the same root await a single render; a failed render is evicted so a
/// cancelled or faulted first caller does not poison the cache for later pages.
/// </summary>
public sealed class NavigationRenderCache
{
	private readonly ConcurrentDictionary<IRootNavigationItem<INavigationModel, INavigationItem>, Lazy<Task<NavigationRenderResult>>> _cache =
		new(ReferenceEqualityComparer.Instance);

	public async Task<NavigationRenderResult> GetOrRenderAsync(
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		Func<Task<NavigationRenderResult>> render)
	{
		var pending = _cache.GetOrAdd(root, _ => new Lazy<Task<NavigationRenderResult>>(render, LazyThreadSafetyMode.ExecutionAndPublication));
		try
		{
			return await pending.Value.ConfigureAwait(false);
		}
		catch (Exception)
		{
			_ = _cache.TryRemove(root, out _);
			throw;
		}
	}
}
