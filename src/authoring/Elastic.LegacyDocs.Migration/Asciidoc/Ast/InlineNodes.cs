// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.LegacyDocs.Migration.Asciidoc.Ast;

public record TextInline(string Text) : IInlineNode;

public record BoldInline(List<IInlineNode> Children) : IInlineNode;

public record ItalicInline(List<IInlineNode> Children) : IInlineNode;

public record MonoInline(string Text) : IInlineNode;

public record AttributeRefInline(string Name) : IInlineNode;

public record InlineLinkNode(string Url, string? Text = null) : IInlineNode;

public record InlineCrossRefNode(string Target, string? Text = null) : IInlineNode;

public record InlineImageNode(string Path, string? Alt = null) : IInlineNode;

public record FootnoteInline(List<IInlineNode> Content) : IInlineNode;

public record SuperscriptInline(List<IInlineNode> Children) : IInlineNode;

public record SubscriptInline(List<IInlineNode> Children) : IInlineNode;

public record LineBreakInline : IInlineNode;
