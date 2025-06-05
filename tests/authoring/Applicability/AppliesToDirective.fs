// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``product availability``.``yaml directive``

open Elastic.Markdown.Myst.Directives
open Elastic.Markdown.Myst.FrontMatter
open authoring
open authoring.MarkdownDocumentAssertions
open Swensen.Unquote
open Xunit
open Elastic.Markdown.Myst.CodeBlocks

type ``piggy back off yaml formatting`` () =
    static let markdown = Setup.Markdown """
```yaml {applies_to}
serverless:
  security: ga 9.0.0
  elasticsearch: beta 9.1.0
  observability: discontinued 9.2.0
```
"""

    [<Fact>]
    let ``parses to AppliesDirective`` () =
        let directives = markdown |> converts "index.md" |> parses<AppliesToCodeBlock>
        test <@ directives.Length = 1 @>

        directives |> appliesToDirective (ApplicableTo(
            Serverless=ServerlessProjectApplicability(
                Security=AppliesCollection.op_Explicit "ga 9.0.0",
                Elasticsearch=AppliesCollection.op_Explicit "beta 9.1.0",
                Observability=AppliesCollection.op_Explicit "discontinued 9.2.0"
            )
        ))

type ``plain block`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
serverless:
  security: ga 9.0.0
  elasticsearch: beta 9.1.0
  observability: discontinued 9.2.0
```
"""

    [<Fact>]
    let ``parses to AppliesDirective`` () =
        let directives = markdown |> converts "index.md" |> parses<AppliesToCodeBlock>
        test <@ directives.Length = 1 @>

        directives |> appliesToDirective (ApplicableTo(
            Serverless=ServerlessProjectApplicability(
                Security=AppliesCollection.op_Explicit "ga 9.0.0",
                Elasticsearch=AppliesCollection.op_Explicit "beta 9.1.0",
                Observability=AppliesCollection.op_Explicit "discontinued 9.2.0"
            )
        ))

type ``warns on old syntax`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
:hosted: all
```
"""
    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``warns on bad syntax`` () =
        markdown |> hasWarning "Applies block does not use valid yaml keys: :hosted"

type ``warns on invalid keys`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
hosted: all
```
"""
    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``warns on bad syntax`` () =
        markdown |> hasWarning "Applies block does not support the following keys: hosted"
