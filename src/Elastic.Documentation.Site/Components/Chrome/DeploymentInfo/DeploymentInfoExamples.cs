// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Chrome.DeploymentInfo;

public static class DeploymentInfoExamples
{
	public static ComponentSection Deployments { get; } = new(
		"Deployment info",
		"Where a preview build came from. It opens on click and keyboard, and on hover with a mouse, and links each row to GitHub.",
		"deployment",
		[
			ComponentExample.Of(
				"Pull request",
				_DeploymentInfo.Create,
				new DeploymentInfoModel
				{
					Branch = "feature/design",
					Commit = "45954b6",
					Repository = "elastic/docs-builder",
					GitHubRef = "refs/pull/123/merge"
				}
			),
			ComponentExample.Of(
				"Branch",
				_DeploymentInfo.Create,
				new DeploymentInfoModel { Branch = "main", Commit = "45954b6", Repository = "nullean/argh", GitHubRef = "refs/heads/main" }
			),
			ComponentExample.Of("Local build", _DeploymentInfo.Create, new DeploymentInfoModel { Branch = "main", Commit = "45954b6" })
		]
	);
}
