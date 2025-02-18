// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``product availability``.``yaml directive``

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
        let directives = markdown |> converts "index.md" |> parses<AppliesToDirective>
        test <@ directives.Length = 1 @>

        directives |> appliesToDirective (ApplicableTo(
            Serverless=ServerlessProjectApplicability(
                Security=ApplicabilityOverTime.op_Explicit "ga 9.0.0",
                Elasticsearch=ApplicabilityOverTime.op_Explicit "beta 9.1.0",
                Observability=ApplicabilityOverTime.op_Explicit "discontinued 9.2.0"
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
        let directives = markdown |> converts "index.md" |> parses<AppliesToDirective>
        test <@ directives.Length = 1 @>

        directives |> appliesToDirective (ApplicableTo(
            Serverless=ServerlessProjectApplicability(
                Security=ApplicabilityOverTime.op_Explicit "ga 9.0.0",
                Elasticsearch=ApplicabilityOverTime.op_Explicit "beta 9.1.0",
                Observability=ApplicabilityOverTime.op_Explicit "discontinued 9.2.0"
            )
        ))