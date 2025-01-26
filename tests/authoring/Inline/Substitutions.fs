module ``inline elements``.``substitutions``

open Xunit
open authoring

type ``read sub from yaml frontmatter`` () =
    static let markdown = Setup.Document """---
sub:
  hello-world: "Hello World!"
---
The following should be subbed: {{hello-world}}
not a comment
"""

    [<Fact>]
    let ``validate HTML: replace substitution`` () =
        markdown |> convertsToHtml """
<p>The following should be subbed: Hello World!<br>
not a comment</p>
        """


type ``requires valid syntax and key to be found`` () =
    static let markdown = Setup.Document """---
sub:
  hello-world: "Hello World!"
---
# Testing substitutions

The following should be subbed: {{hello-world}}
not a comment
not a {{valid-key}}
not a {substitution}
"""

    [<Fact>]
    let ``emits an error when sub key is not found`` () =
        markdown |> hasError "key {valid-key} is undefined"

    [<Fact>]
    let ``validate HTML: leaves non subs alone`` () =
        markdown |> convertsToHtml """
 <p>The following should be subbed: Hello World!<br>
 	not a comment</br>
 	not a {{valid-key}}<br>
 	not a {substitution}</p>
"""
