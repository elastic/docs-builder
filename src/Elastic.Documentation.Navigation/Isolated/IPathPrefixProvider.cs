// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation.Navigation.Assembler;

namespace Elastic.Documentation.Navigation.Isolated;

public interface INavigationHomeProvider
{
	string PathPrefix { get; }
	IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }
	string Id { get; }
}

public interface INavigationHomeAccessor
{
	INavigationHomeProvider HomeProvider { get; set; }
}

[DebuggerDisplay("{PathPrefix} => {NavigationRoot.Url}")]
public class NavigationHomeProvider(string pathPrefix, IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot) : INavigationHomeProvider
{
	/// <inheritdoc />
	public string PathPrefix { get; } = pathPrefix;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = navigationRoot;

	public string Id { get; } = Guid.NewGuid().ToString("N");

	public override string ToString() => $"{PathPrefix} => {NavigationRoot.Url}";
}

