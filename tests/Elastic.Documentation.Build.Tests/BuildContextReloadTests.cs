// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;

namespace Elastic.Documentation.Build.Tests;

public class BuildContextReloadTests
{
	private const string DocsetPath = "docs/docset.yml";

	[Test]
	public void ReloadConfiguration_AppliesTheEditedYaml_KeepsServeOverrides_AndHonorsInjectedEnvironment()
	{
		var fileSystem = new MockFileSystem(
			new Dictionary<string, MockFileData>
			{
				[DocsetPath] = new MockFileData("project: test\ntoc:\n- file: index.md\n"),
				["docs/index.md"] = new MockFileData("# Home")
			},
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);
		_ = fileSystem.Directory.CreateDirectory(Path.Join(Paths.WorkingDirectoryRoot.FullName, ".git"));
		var invocation = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var environment = new MockEnvironmentVariables();
		environment.Set("FEATURE_GUIDE_NAV", "true");
		environment.Set("FEATURE_DISABLE_GITHUB_EDIT_LINK", "false");
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(invocation, new DocumentationScopeOptions { Inner = fileSystem }),
			TestHelpers.CreateConfigurationContext(fileSystem),
			environment
		);
		context.OverrideFeatures(f => f.With("diagnostics-panel", true).With("page-feedback", true));
		context.Configuration.Features.PrimaryNavEnabled.Should().BeFalse();

		fileSystem.File.WriteAllText(
			Path.Join(Paths.WorkingDirectoryRoot.FullName, DocsetPath),
			"project: test\nfeatures:\n  primary-nav: true\n  disable-github-edit-link: true\ntoc:\n- file: index.md\n"
		);
		context.ReloadConfiguration();

		var features = context.Configuration.Features;
		features.PrimaryNavEnabled.Should().BeTrue("the edited docset.yml takes effect");
		features.DiagnosticsPanelEnabled.Should().BeTrue("serve's HUD override survives a reload");
		features.PageFeedbackEnabled.Should().BeTrue("the page-feedback override survives a reload");
		features.GuideNavEnabled.Should().BeTrue("the context's injected FEATURE_GUIDE_NAV is still honored");
		features.DisableGitHubEditLink.Should().BeFalse("the injected FEATURE_DISABLE_GITHUB_EDIT_LINK=false wins over the edited yaml");
	}
}
