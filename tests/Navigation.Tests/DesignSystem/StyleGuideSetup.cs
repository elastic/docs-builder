// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using Elastic.Markdown.StyleGuide;

namespace Elastic.Documentation.Navigation.Tests.DesignSystem;

internal static class StyleGuideSetup
{
	// The style guide's test data is read while tests are discovered, so the sections that live in Elastic.Markdown are added as the
	// assembly loads, as the docs-builder host does when it starts the style guide.
	[ModuleInitializer]
	internal static void RegisterMarkdownComponents() => MarkdownComponentSections.Register();
}
