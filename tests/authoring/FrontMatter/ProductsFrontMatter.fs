// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``AuthoringTests``.``frontmatter``.``products frontmatter``

open Swensen.Unquote
open Xunit
open authoring
open JetBrains.Annotations

let frontMatter ([<LanguageInjection("yaml")>]m: string) =
    Setup.Document $"""---
{m}
---
# Test Page

This is a test page with products frontmatter.
"""

/// Helper to create a docset with elasticsearch as the default product (matching hardcoded test data in Setup.fs)
let withDocsetProducts ([<LanguageInjection("markdown")>]m: string) =
    let options = { SetupOptions.Empty with DocsetProducts = Some ["elasticsearch"] }
    Setup.GenerateWithOptions options [ TestFile.Index m ]

type ``products frontmatter in HTML`` () =
    static let markdownWithProducts = frontMatter """
products:
  - id: elasticsearch
  - id: ecctl
"""

    [<Fact>]
    let ``includes products meta tags when products are specified`` () =
        markdownWithProducts |> converts "index.md" |> containsHtml """
<meta class="elastic" name="product_name" content="Elasticsearch,Elastic Cloud Control ECCTL"/>
<meta name="DC.subject" content="Elasticsearch,Elastic Cloud Control ECCTL"/>
"""

    [<Fact>]
    let ``does not include products meta tags when no products are specified`` () =
        let markdownWithoutProducts = Setup.Document """
# Test Page

This is a test page without products frontmatter.
"""
        // When there are no products, no product-related meta tags should be rendered at all
        let results = markdownWithoutProducts.Value
        let defaultFile = results.MarkdownResults |> Seq.find (fun r -> r.File.RelativePath = "index.md")
        let html = defaultFile.Html
        
        // Verify that product meta tags are NOT present in the HTML
        test <@ not (html.Contains("product_name")) @>
        test <@ not (html.Contains("DC.subject")) @>

type ``products frontmatter in LLM Markdown`` () =
    static let markdownWithProducts = frontMatter """
products:
  - id: elasticsearch
  - id: ecctl
"""

    static let markdownWithoutProducts = Setup.Document """
# Test Page

This is a test page without products frontmatter.
"""

    [<Fact>]
    let ``includes products in frontmatter when products are specified`` () =
        // Test that the products frontmatter is correctly processed by checking the file
        let results = markdownWithProducts.Value
        let defaultFile = results.MarkdownResults |> Seq.find (fun r -> r.File.RelativePath = "index.md")
        
        // Test that the file has the correct products
        test <@ defaultFile.File.YamlFrontMatter <> null @>
        match defaultFile.File.YamlFrontMatter with
        | NonNull yamlFrontMatter ->
            test <@ yamlFrontMatter.Products <> null @>
            match yamlFrontMatter.Products with
            | NonNull products ->
                test <@ products.Count = 2 @>
                // Test that the products are correctly identified
                let productIds = products |> Seq.map _.Id |> Set.ofSeq
                test <@ productIds.Contains("elasticsearch") @>
                test <@ productIds.Contains("ecctl") @>
            | _ -> ()
        | _ -> ()
        

    [<Fact>]
    let ``does not include products in frontmatter when no products are specified`` () =
        // Test that pages without products frontmatter don't have products
        let results = markdownWithoutProducts.Value
        let defaultFile = results.MarkdownResults |> Seq.find (fun r -> r.File.RelativePath = "index.md")
        
        // Test that the file has no products
        match defaultFile.File.YamlFrontMatter with
        | NonNull frontMatter ->
            match frontMatter.Products with
            | NonNull products -> 
                test <@ products.Count = 0 @>
            | _ -> ()
        | _ -> ()

/// Helper to create a docset with elasticsearch as the default product (matching hardcoded test data in Setup.fs)
let withDocsetProducts ([<LanguageInjection("markdown")>]m: string) =
    let options = { SetupOptions.Empty with DocsetProducts = Some ["elasticsearch"] }
    Setup.GenerateWithOptions options [ TestFile.Index m ]
/// Tests for docset-level products merging with frontmatter products
type ``docset products merging`` () =
    
    static let docsetOnly = withDocsetProducts """
# Test Page

This is a test page without frontmatter products.
"""
    
    static let docsetAndFrontmatter = withDocsetProducts """---
products:
  - id: ecctl
---
# Test Page

This page has both docset and frontmatter products.
"""

    [<Fact>]
    let ``docset products appear in HTML when no frontmatter products`` () =
        let results = docsetOnly.Value
        let defaultFile = results.MarkdownResults |> Seq.find (fun r -> r.File.RelativePath = "index.md")
        let html = defaultFile.Html
        
        // The docset has elasticsearch, so it should appear in the meta tags
        // This test should FAIL without the fix because docset products are not merged
        test <@ html.Contains("""<meta class="elastic" name="product_name" content="Elasticsearch"/>""") @>

    [<Fact>]
    let ``docset products merge with frontmatter products`` () =
        docsetAndFrontmatter |> converts "index.md" |> containsHtml """
<meta class="elastic" name="product_name" content="Elasticsearch,Elastic Cloud Control ECCTL"/>
<meta name="DC.subject" content="Elasticsearch,Elastic Cloud Control ECCTL"/>
"""
