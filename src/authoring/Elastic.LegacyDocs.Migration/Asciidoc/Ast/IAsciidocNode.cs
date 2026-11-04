// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.LegacyDocs.Migration.Asciidoc.Ast;

public interface IAsciidocNode;

public interface IInlineNode;

public interface IBlockNode : IAsciidocNode;

public record AnchoredBlock(string Id, IAsciidocNode Inner) : IBlockNode;
