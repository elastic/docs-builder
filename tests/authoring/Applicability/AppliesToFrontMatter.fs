// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``product availability``.``yaml frontmatter``

open Elastic.Documentation.AppliesTo
open JetBrains.Annotations
open Xunit
open authoring
open authoring.MarkdownDocumentAssertions

let frontMatter ([<LanguageInjection("yaml")>]m: string) =
    Setup.Document $"""---
{m}
---
# Document
"""

type ``apply defaults to all`` () =
    static let markdown = frontMatter """
applies_to:
"""
    [<Fact>]
    let ``apply matches expected`` () =
        markdown |> appliesTo (Unchecked.defaultof<ApplicableTo>)

type ``apply default to top level arguments`` () =
    static let markdown = frontMatter """
applies_to:
   deployment:
   serverless:
"""
    [<Fact>]
    let ``apply matches expected`` () =
        markdown |> appliesTo (ApplicableTo(
            Deployment=DeploymentApplicability.All,
            Serverless=ServerlessProjectApplicability.All
        ))

type ``parses serverless as string to set all projects`` () =
    static let markdown = frontMatter """
applies_to:
   serverless: ga 9.0.0
"""
    [<Fact>]
    let ``apply matches expected`` () =
        let expectedAvailability = AppliesCollection.op_Explicit "ga 9.0.0"
        markdown |> appliesTo (ApplicableTo(
            Serverless=ServerlessProjectApplicability(
                Elasticsearch=expectedAvailability,
                Observability=expectedAvailability,
                Security=expectedAvailability
            )
        ))

type ``parses serverless projects`` () =
    static let markdown = frontMatter """
applies_to:
   serverless:
      security: ga 9.0.0
      elasticsearch: beta 9.1.0
      observability: removed 9.2.0
"""
    [<Fact>]
    let ``apply matches expected`` () =
        markdown |> appliesTo (ApplicableTo(
            Serverless=ServerlessProjectApplicability(
                Security=AppliesCollection.op_Explicit "ga 9.0.0",
                Elasticsearch=AppliesCollection.op_Explicit "beta 9.1.0",
                Observability=AppliesCollection.op_Explicit "removed 9.2.0"
            )
        ))

type ``parses stack`` () =
    static let markdown = frontMatter """
applies_to:
   stack: ga 9.1
"""
    [<Fact>]
    let ``apply matches expected`` () =
        markdown |> appliesTo (ApplicableTo(
            Stack=AppliesCollection.op_Explicit "ga 9.1.0"
        ))

type ``parses deployment as string to set all deployment targets`` () =
    static let markdown = frontMatter """
applies_to:
   deployment: ga 9.0.0
"""
    [<Fact>]
    let ``apply matches expected`` () =
        let expectedAvailability = AppliesCollection.op_Explicit "ga 9.0.0"
        markdown |> appliesTo (ApplicableTo(
            Deployment=DeploymentApplicability(
                Eck=expectedAvailability,
                Ess=expectedAvailability,
                Ece=expectedAvailability,
                Self=expectedAvailability
            )
        ))

type ``parses deployment types as individual properties`` () =
    static let markdown = frontMatter """
applies_to:
   deployment:
      eck: ga 9.0
      ess: beta 9.1
      ece: removed 9.2.0
      self: unavailable 9.3.0
"""
    [<Fact>]
    let ``apply matches expected`` () =
        markdown |> appliesTo (ApplicableTo(
            Deployment=DeploymentApplicability(
                Eck=AppliesCollection.op_Explicit "ga 9.0",
                Ess=AppliesCollection.op_Explicit "beta 9.1",
                Ece=AppliesCollection.op_Explicit "removed 9.2.0",
                Self=AppliesCollection.op_Explicit "unavailable 9.3.0"
            )
        ))

type ``parses product coming DEPRECATED`` () =
    static let markdown = frontMatter """
applies_to:
   product: coming 9.5
"""

    [<Fact>]
    let ``should warn of deprecated lifecycle state`` () =
        markdown |> hasHint "The 'coming' lifecycle is deprecated and will be removed"

type ``parses product planned`` () =
    static let markdown = frontMatter """
applies_to:
   product: planned 9.5
"""

    [<Fact>]
    let ``should warn of deprecated lifecycle state`` () =
        markdown |> hasHint "The 'planned' lifecycle is deprecated and will be removed"

type ``parses product removed`` () =
    static let markdown = frontMatter """
applies_to:
   product: removed 9.5
"""

    [<Fact>]
    let ``apply matches expected`` () =
        markdown |> appliesTo (ApplicableTo(
            Product=AppliesCollection([
                Applicability.op_Explicit "removed 9.5";
            ] |> Array.ofList)
        ))

type ``parses product multiple`` () =
    static let markdown = frontMatter """
applies_to:
   product: preview 9.5, removed 9.7
"""
    [<Fact>]
    let ``apply matches expected`` () =
        markdown |> appliesTo (ApplicableTo(
            Product=AppliesCollection([
                Applicability.op_Explicit "removed 9.7";
                Applicability.op_Explicit "preview 9.5"
            ] |> Array.ofList)
        ))

type ``lenient to defining types at top level`` () =
    static let markdown = frontMatter """
applies_to:
  eck: ga 9.0
  ess: beta 9.1
  ece: removed 9.2.0
  self: unavailable 9.3.0
  security: ga 9.0.0
  elasticsearch: beta 9.1.0
  observability: removed 9.2.0
  product: preview 9.5, removed 9.7
  apm_agent_dotnet: ga 9.0
  ecctl: ga 10.0
  stack: ga 9.1
"""
    [<Fact>]
    let ``apply matches expected`` () =
        markdown |> appliesTo (ApplicableTo(
            Deployment=DeploymentApplicability(
                Eck=AppliesCollection.op_Explicit "ga 9.0",
                Ess=AppliesCollection.op_Explicit "beta 9.1",
                Ece=AppliesCollection.op_Explicit "removed 9.2.0",
                Self=AppliesCollection.op_Explicit "unavailable 9.3.0"
            ),
            Serverless=ServerlessProjectApplicability(
                Security=AppliesCollection.op_Explicit "ga 9.0.0",
                Elasticsearch=AppliesCollection.op_Explicit "beta 9.1.0",
                Observability=AppliesCollection.op_Explicit "removed 9.2.0"
            ),
            Stack=AppliesCollection.op_Explicit "ga 9.1.0",
            Product=AppliesCollection.op_Explicit "preview 9.5, removed 9.7",
            ProductApplicability=ProductApplicability(
                ApmAgentDotnet=AppliesCollection.op_Explicit "ga 9.0",
                Ecctl=AppliesCollection.op_Explicit "ga 10.0"
            )
        ))

type ``parses empty applies_to as null`` () =
    static let markdown = frontMatter """
applies_to:
"""
    [<Fact>]
    let ``does not render label`` () =
        markdown |> appliesTo (Unchecked.defaultof<ApplicableTo>)

type ``sorts applies_to versions in descending order`` () =
    static let markdown = frontMatter """
applies_to:
   stack: ga 8.18.6, ga 9.1.2, ga 8.19.2, ga 9.0.6
"""
    [<Fact>]
    let ``versions are sorted highest to lowest`` () =
        let expectedVersions = [
            Applicability.op_Explicit "ga 9.1.2"
            Applicability.op_Explicit "ga 9.0.6"
            Applicability.op_Explicit "ga 8.19.2"
            Applicability.op_Explicit "ga 8.18.6"
        ]
        markdown |> appliesTo (ApplicableTo(
            Stack=AppliesCollection(expectedVersions |> Array.ofList)
        ))

type ``sorts applies_to with mixed versioned and non-versioned items`` () =
    static let markdown = frontMatter """
applies_to:
   stack: ga 8.18.6, ga, ga 9.1.2, all, ga 8.19.2
"""
    [<Fact>]
    let ``versioned items are sorted first, non-versioned items last`` () =
        let expectedVersions = [
            Applicability.op_Explicit "ga 9.1.2"
            Applicability.op_Explicit "ga 8.19.2"
            Applicability.op_Explicit "ga 8.18.6"
            Applicability.op_Explicit "ga"
            Applicability.op_Explicit "all"
        ]
        markdown |> appliesTo (ApplicableTo(
            Stack=AppliesCollection(expectedVersions |> Array.ofList)
        ))

type ``sorts applies_to with patch versions correctly`` () =
    static let markdown = frontMatter """
applies_to:
   stack: ga 9.1, ga 9.1.1, ga 9.0.5
"""
    [<Fact>]
    let ``patch versions are sorted correctly`` () =
        let expectedVersions = [
            Applicability.op_Explicit "ga 9.1.1"
            Applicability.op_Explicit "ga 9.1"
            Applicability.op_Explicit "ga 9.0.5"
        ]
        markdown |> appliesTo (ApplicableTo(
            Stack=AppliesCollection(expectedVersions |> Array.ofList)
        ))

type ``sorts applies_to with major versions correctly`` () =
    static let markdown = frontMatter """
applies_to:
   stack: ga 3.x, ga 5.x
"""
    [<Fact>]
    let ``major versions are sorted correctly`` () =
        let expectedVersions = [
            Applicability.op_Explicit "ga 5.x"
            Applicability.op_Explicit "ga 3.x"
        ]
        markdown |> appliesTo (ApplicableTo(
            Stack=AppliesCollection(expectedVersions |> Array.ofList)
        ))
