// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Serverless;

/// <summary>Puts the bundles of a preview run in the product folder, where the site build reads them.</summary>
public static class ServerlessPreviewBundles
{
	/// <summary>
	/// Replaces the bundles of an earlier run of <paramref name="svc"/> in the folder of its product under <paramref name="rootOutput"/>
	/// with the files of <paramref name="pending"/>, or with nothing when <paramref name="pending"/> is <c>null</c>, as when production caught
	/// up with staging. Only the files named by <see cref="ServerlessPromotion.PreviewBundleFileNames"/> go, because the bundles of another
	/// service share the folder. A folder left empty is removed.
	/// </summary>
	public static void Replace(ServerlessService svc, DirectoryInfo rootOutput, DirectoryInfo? pending, ILogger logger)
	{
		var productOutput = new DirectoryInfo(Path.Combine(rootOutput.FullName, svc.Product));
		foreach (var name in ServerlessPromotion.PreviewBundleFileNames(svc))
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
