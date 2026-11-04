// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Components.PropertyTree._Partials;
using Elastic.ApiExplorer.Model;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class SchemaTypeRenderingTests
{
	private static CancellationToken Ct => TestContext.Current!.Execution.CancellationToken;

	[Test]
	public async Task Render_ConstraintWithTitle_IsKeyboardFocusable()
	{
		var annotation = new TypeAnnotation([new TypeSpan("pattern: long", SchemaHelpers.ConstraintCssClass, "pattern: long")]);

		var html = await _SchemaType.Create(annotation).RenderAsync(cancellationToken: Ct);

		html.Should().Contain("tabindex=\"0\"");
	}

	[Test]
	public async Task Render_ConstraintWithoutTitle_IsNotFocusable()
	{
		var annotation = new TypeAnnotation([new TypeSpan("min: 1", SchemaHelpers.ConstraintCssClass)]);

		var html = await _SchemaType.Create(annotation).RenderAsync(cancellationToken: Ct);

		html.Should().NotContain("tabindex");
	}
}
