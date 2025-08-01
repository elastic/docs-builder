// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``product availability``.``yaml directive``

open Elastic.Markdown.Myst.Directives.AppliesTo
open Elastic.Markdown.Myst.FrontMatter
open authoring
open authoring.MarkdownDocumentAssertions
open Swensen.Unquote
open Xunit

type ``piggy back off yaml formatting`` () =
    static let markdown = Setup.Markdown """
```yaml {applies_to}
serverless:
  security: ga 9.0.0
  elasticsearch: beta 9.1.0
  observability: removed 9.2.0
```
"""

    [<Fact>]
    let ``parses to AppliesDirective`` () =
        let directives = markdown |> converts "index.md" |> parses<AppliesToDirective>
        test <@ directives.Length = 1 @>

        directives |> appliesToDirective (ApplicableTo(
            Serverless=ServerlessProjectApplicability(
                Security=AppliesCollection.op_Explicit "ga 9.0.0",
                Elasticsearch=AppliesCollection.op_Explicit "beta 9.1.0",
                Observability=AppliesCollection.op_Explicit "removed 9.2.0"
            )
        ))

type ``plain block`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
serverless:
  security: ga 9.0.0
  elasticsearch: beta 9.1.0
  observability: removed 9.2.0
apm_agent_dotnet: ga 9.0
apm_agent_node: ga 10.0
```
"""

    [<Fact>]
    let ``parses to AppliesDirective`` () =
        let directives = markdown |> converts "index.md" |> parses<AppliesToDirective>
        test <@ directives.Length = 1 @>

        directives |> appliesToDirective (ApplicableTo(
            Serverless=ServerlessProjectApplicability(
                Security=AppliesCollection.op_Explicit "ga 9.0.0",
                Elasticsearch=AppliesCollection.op_Explicit "beta 9.1.0",
                Observability=AppliesCollection.op_Explicit "removed 9.2.0"
            ),
            ProductApplicability=ProductApplicability(
                ApmAgentDotnet=AppliesCollection.op_Explicit "ga 9.0",
                ApmAgentNode=AppliesCollection.op_Explicit "ga 10.0"
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
