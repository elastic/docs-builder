// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.IO.Navigation;

namespace Documentation.Assembler.Navigation;

#pragma warning disable CS9113 // Parameter is unread.
public class GlobalNavigationHtmlWriter(
	GlobalNavigationFile navigationFile,
	AssembleContext assembleContext,
	GlobalNavigation globalNavigation,
	AssembleSources assembleSources
) : INavigationHtmlWriter
#pragma warning restore CS9113 // Parameter is unread.
{
	private readonly ConcurrentDictionary<(string, int), string> _renderedNavigationCache = [];

	private ImmutableHashSet<Uri> Phantoms { get; } = [.. navigationFile.Phantoms.Select(p => p.Source)];

	public async Task<NavigationRenderResult> RenderNavigation(IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation, int maxLevel, Cancel ctx = default)
	{
		await Task.CompletedTask;
		INodeNavigationItem<INavigationModel, INavigationItem> lastParentBeforeRoot = currentRootNavigation;
		INodeNavigationItem<INavigationModel, INavigationItem> parent = currentRootNavigation;
		while (parent.Parent is not null)
		{
			lastParentBeforeRoot = parent;
			parent = parent.Parent;
		}
		if (_renderedNavigationCache.TryGetValue((lastParentBeforeRoot.Id, maxLevel), out var html))
		{
			return new NavigationRenderResult
			{
				Html = html,
				Id = lastParentBeforeRoot.Id
			};
		}

		Console.WriteLine($"Rendering navigation for {lastParentBeforeRoot.NavigationTitle} ({lastParentBeforeRoot.Id})");
		if (lastParentBeforeRoot.NavigationTitle == "Cloud release notes")
		{
		}

		if (lastParentBeforeRoot is not DocumentationGroup group)
			return NavigationRenderResult.Empty;

		var model = CreateNavigationModel(group, maxLevel);
		html = await ((INavigationHtmlWriter)this).Render(model, ctx);
		_renderedNavigationCache[(lastParentBeforeRoot.Id, maxLevel)] = html;
		return new NavigationRenderResult
		{
			Html = html,
			Id = lastParentBeforeRoot.Id
		};
	}

	private NavigationViewModel CreateNavigationModel(DocumentationGroup group, int maxLevel)
	{
		var topLevelItems = globalNavigation.TopLevelItems;
		return new NavigationViewModel
		{
			Title = group.Index.NavigationTitle,
			TitleUrl = group.Index.Url,
			Tree = group,
			IsPrimaryNavEnabled = true,
			IsUsingNavigationDropdown = true,
			IsGlobalAssemblyBuild = true,
			TopLevelItems = topLevelItems,
			MaxLevel = maxLevel
		};
	}
}
