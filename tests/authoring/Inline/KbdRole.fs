// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``inline elements``.``kbd role``

open Xunit
open authoring

type ``renders single kbd role`` () =
    static let markdown = Setup.Markdown """
{kbd}`cmd`
"""
    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
<p><kbd class="kbd" aria-label="Command"><span class="kbd-icon">⌘</span>Cmd</kbd></p>
"""

type ``renders single character kbd role`` () =
    static let markdown = Setup.Markdown """
{kbd}`c`
"""
    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
<p><kbd class="kbd">c</kbd></p>
"""

type ``renders combined kbd role`` () =
    static let markdown = Setup.Markdown """
{kbd}`cmd+shift+c`
"""
    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
<p><kbd class="kbd" aria-label="Command"><span class="kbd-icon">⌘</span>Cmd</kbd> + <kbd class="kbd"><span class="kbd-icon">⇧</span>Shift</kbd> + <kbd class="kbd">c</kbd></p>
"""

type ``renders combined kbd role with special characters`` () =
    static let markdown = Setup.Markdown """
{kbd}`ctrl+alt+del`
"""
    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
<p><kbd class="kbd" aria-label="Control"><span class="kbd-icon">⌃</span>Ctrl</kbd> + <kbd class="kbd"><span class="kbd-icon">⌥</span>Alt</kbd> + <kbd class="kbd" aria-label="Delete">Del</kbd></p>
"""

type ``renders alternative kbd role`` () =
    static let markdown = Setup.Markdown """
{kbd}`ctrl|cmd+c`
"""
    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
<p><kbd class="kbd" aria-label="Control or Command"><span class="kbd-icon">⌃</span>Ctrl / <span class="kbd-icon">⌘</span>Cmd</kbd> + <kbd class="kbd">c</kbd></p>
"""

type ``renders plus kbd role`` () =
    static let markdown = Setup.Markdown """
{kbd}`plus`
"""
    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
<p><kbd class="kbd">+</kbd></p>
"""

type ``renders pipe kbd role`` () =
    static let markdown = Setup.Markdown """
{kbd}`pipe`
"""
    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
<p><kbd class="kbd">|</kbd></p>
"""
