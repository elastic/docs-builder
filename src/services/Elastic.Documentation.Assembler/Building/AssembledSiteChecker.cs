// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Publishing;

namespace Elastic.Documentation.Assembler.Building;

/// <summary>
/// The one step of an assembler build that checks what spans repositories. It builds the <see cref="AssembledSite"/> (every repository
/// the assembler builds, with the navigation that places them) and the <see cref="NavigationInAssembly"/> pair from what the build
/// already loaded, checks each once, and returns every finding in one <see cref="Result{T}"/>.
/// </summary>
/// <remarks>
/// Not built here: the <c>LinkGraph</c> aggregate. It is per repository and needs that repository's own <c>links.json</c>, which
/// only exists after the repository has been built, so it cannot be checked before the build starts.
/// </remarks>
public static class AssembledSiteChecker
{
	public static Result<AssembledSite> Check(AssembleContext context, AssembleSources sources)
	{
		var repositories = sources
			.AssembleSets
			.Values
			.Select(set =>
			{
				var name = set.Checkout.Repository.Name;
				var prefix = sources.NavigationTocMappings.TryGetValue(new Uri($"{name}://"), out var mapping)
					? mapping.SourcePathPrefix
					: string.Empty;
				return new AssembledRepository(name, prefix, set.BuildContext.Definition);
			})
			.ToList();

		var navigationFile = context.NavigationFile.FullName;
		var inAssembly = NavigationInAssemblyConstraints.All.Report(new NavigationInAssembly(context.Navigation, context.Configuration));
		var site = new AssembledSite(repositories, context.Navigation);
		return Result.From(AssembledSiteConstraints.All.Report(site), navigationFile, inAssembly.ToDiagnostics(navigationFile));
	}
}
