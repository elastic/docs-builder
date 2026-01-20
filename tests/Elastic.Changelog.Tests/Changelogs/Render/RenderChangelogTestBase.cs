// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Bundling;
using Elastic.Changelog.Creation;
using Elastic.Changelog.Rendering;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public abstract class RenderChangelogTestBase : ChangelogTestBase
{
	protected ChangelogRenderingService Service { get; }

	protected RenderChangelogTestBase(ITestOutputHelper output) : base(output) =>
		Service = new ChangelogRenderingService(LoggerFactory, ConfigurationContext, FileSystem);
}
