// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Mcp.Remote;

namespace Mcp.Remote.Tests;

public class McpToolAnnotationTests
{
	[Test]
	public void AllTools_AreAnnotatedReadOnly()
	{
		foreach (var profile in new[] { McpServerProfile.Public, McpServerProfile.Internal })
		{
			var tools = McpToolRegistration.CreatePrefixedTools(profile).ToList();
			tools.Should().NotBeEmpty();

			foreach (var tool in tools)
			{
				var annotations = tool.ProtocolTool.Annotations;
				annotations.Should().NotBeNull($"{tool.ProtocolTool.Name} must declare annotations");
				annotations.ReadOnlyHint.Should().BeTrue($"{tool.ProtocolTool.Name} only reads data");
				annotations.IdempotentHint.Should().BeTrue($"{tool.ProtocolTool.Name} is idempotent");
				annotations.DestructiveHint.Should().BeFalse($"{tool.ProtocolTool.Name} is not destructive");
				annotations.OpenWorldHint.Should().BeFalse($"{tool.ProtocolTool.Name} only queries Elastic docs");
			}
		}
	}
}
