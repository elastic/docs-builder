// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Serverless;

/// <summary>Puts the bundles of a preview run in the product folder, where the site build reads them.</summary>
public static class ServerlessPreviewBundles
{
	/// <summary>
	/// Removes the <c>head</c> bundles of every service under <paramref name="rootOutput"/>, which a folder from before the rename to
	/// Edge still holds. See <see cref="ServerlessPromotion.LegacyBundleFileNames"/>. A folder left empty is removed.
	/// </summary>
	public static void RemoveLegacy(DirectoryInfo rootOutput, ILogger logger)
	{
		foreach (var (product, fileName) in ServerlessPromotion.LegacyBundleFileNames())
		{
			var path = Path.Combine(rootOutput.FullName, product, fileName);
			if (!File.Exists(path))
				continue;
			logger.LogInformation("Removing a bundle of the release that was called head: {Path}", path);
			File.Delete(path);
		}

		foreach (var product in ServerlessPromotion.LegacyBundleFileNames().Select(l => l.Product).Distinct(StringComparer.Ordinal))
		{
			var folder = new DirectoryInfo(Path.Combine(rootOutput.FullName, product));
			if (folder.Exists && !folder.EnumerateFileSystemInfos().Any())
				folder.Delete();
		}
	}

	/// <summary>
	/// Replaces the bundles of an earlier run of <paramref name="svc"/> in the folder of its product under <paramref name="rootOutput"/>
	/// with the files of <paramref name="pending"/>, or with nothing when <paramref name="pending"/> is <c>null</c>, as when production caught
	/// up with staging. Only the files named by <see cref="ServerlessPromotion.PreviewBundleFileNames"/> go, because the bundles of another
	/// service share the folder. A folder left empty is removed.
	/// </summary>
	public static void Replace(ServerlessService svc, DirectoryInfo rootOutput, DirectoryInfo? pending, ILogger logger)
	{
		var productOutput = new DirectoryInfo(Path.Combine(rootOutput.FullName, svc.Product));
		// The bundle of the release that was called head goes with the first of the new ones: the service has an Edge bundle now, or none.
		foreach (var name in ServerlessPromotion.PreviewBundleFileNames(svc).Concat(ServerlessPromotion.LegacyBundleFileNames(svc)))
		{
			var path = Path.Combine(productOutput.FullName, name);
			if (!File.Exists(path))
				continue;
			logger.LogInformation("Removing the bundle of an earlier run: {Path}", path);
			File.Delete(path);
		}

		foreach (var file in pending?.EnumerateFiles() ?? [])
		{
			_ = Directory.CreateDirectory(productOutput.FullName);
			file.MoveTo(Path.Combine(productOutput.FullName, file.Name), overwrite: true);
		}

		if (productOutput.Exists && !productOutput.EnumerateFileSystemInfos().Any())
			productOutput.Delete();
	}
}
