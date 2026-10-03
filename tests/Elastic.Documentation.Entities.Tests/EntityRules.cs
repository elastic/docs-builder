// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Catalog;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Publishing;
using Nullean.Entities;

namespace Elastic.Documentation.Entities.Tests;

/// <summary>The navigation together with the assembly whose repositories it mounts: the pair a rule over both is written against.</summary>
public sealed record NavigationInAssembly(SiteNavigationDefinition Navigation, AssemblyConfiguration Assembly);

/// <summary>The rules the domain-model investigation lists as unchecked or checked late, as Nullean.Entities constraint sets over the real aggregates.</summary>
public static class EntityRules
{
	/// <summary>The three name-keyed dictionaries repeat <c>Repository.Name</c> as their key, and the derived ones are subsets of each other.</summary>
	public static readonly ConstraintSet<AssemblyConfiguration> Assembly = ConstraintSet.From(
		("reference-keys-match-names", Constraints<AssemblyConfiguration>.KeysMatch(a => a.ReferenceRepositories, r => r.Name)),
		("available-keys-match-names", Constraints<AssemblyConfiguration>.KeysMatch(a => a.AvailableRepositories, r => r.Name)),
		("private-keys-match-names", Constraints<AssemblyConfiguration>.KeysMatch(a => a.PrivateRepositories, r => r.Name)),
		("private-is-available", Constraints<AssemblyConfiguration>.References<KeyValuePair<string, Repository>, string>(
			a => a.PrivateRepositories,
			kv => [kv.Key],
			a => a.AvailableRepositories.Keys
		)),
		("private-is-listed", Constraints<AssemblyConfiguration>.References<KeyValuePair<string, Repository>, string>(
			a => a.PrivateRepositories,
			kv => [kv.Key],
			a => a.AllPrivateRepositoryNames
		)),
		("available-repositories-publish", Constraints<AssemblyConfiguration>.Always(
			a => a.AvailableRepositories.Values,
			repositories => repositories.All(r => !r.Skip),
			"An available repository does not skip publishing."
		))
	);

	/// <summary>A mount refers to a repository the assembly publishes. Nothing in the domain checks it today; the navigation build throws "Could not find navigation node".</summary>
	public static readonly ConstraintSet<NavigationInAssembly> Mounts = ConstraintSet.From(
		("mount-names-an-available-repository", Constraints<NavigationInAssembly>.References<SiteTableOfContentsRef, string>(
			p => SiteNavigationConstraints.AllMounts(p.Navigation),
			mount => [mount.Source.Scheme],
			p => p.Assembly.AvailableRepositories.Keys
		))
	);

	/// <summary>D22: a docset's product ids exist in the catalog. The resolver drops unknown ones silently.</summary>
	public static readonly ConstraintSet<DocumentationSetInCatalog> Products = ConstraintSet.From(
		("docset-products-exist", Constraints<DocumentationSetInCatalog>.References<string, string>(
			p => p.Docset.Products,
			id => [id],
			p => p.Products.Products.Keys
		))
	);

	/// <summary>D74: a path appears once in the table of contents, at any depth (the first one wins today, silently).</summary>
	public static readonly ConstraintSet<DocumentationSetDefinition> Toc = ConstraintSet.From(
		("toc-lists-no-path-twice", Constraints<DocumentationSetDefinition>.Unique(
			d => d.TableOfContents.Descendants().Where(i => i.Value is not null).Select(i => i.Item()),
			item => (string)item.PathRelativeToDocumentationSet
		))
	);
}
