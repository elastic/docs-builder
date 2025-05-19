module ``linters``.``white space normalizers``

open Xunit
open authoring


type ``white space detection`` () =

    static let markdown = Setup.Markdown $"""
not a{'\u000B'}space
"""

    [<Fact>]
    let ``validate HTML: should not contain bad space character`` () =
        markdown |> convertsToHtml """<p>not a space</p>"""

    [<Fact>]
    let ``emits a hint when a bad space is used`` () =
        markdown |> hasHint "Irregular whitespace character detected: U+000B (Line Tabulation (VT)). This may impair Markdown rendering."
