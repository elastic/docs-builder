// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Codex.Landing;
using Elastic.Documentation.Site;

namespace Elastic.Codex.StyleGuide;

/// <summary>A Codex page for the style guide's previews: the real layout around placeholder content. Exactly one of the contents is set.</summary>
public sealed record CodexPreviewModel(GlobalLayoutViewModel Layout, CodexLandingContent? Landing = null, CodexGroupContent? Group = null);
