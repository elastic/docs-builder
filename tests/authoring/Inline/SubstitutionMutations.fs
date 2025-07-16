// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``inline elements``.``substitution mutations``

open Xunit
open authoring

type ``read sub from yaml frontmatter`` () =
    let markdown = Setup.Document """---
sub:
  hello-world: "Hello world!"
  versions.stack: 9.1.0
---
* Lowercase: {{hello-world | lc}}
* Uppercase: {{hello-world | uc}}
* TitleCase: {{hello-world | tc}}
* kebab-case: {{hello-world | kc}}
* camelCase: {{hello-world | tc | cc}}
* PascalCase: {{hello-world | pc}}
* SnakeCase: {{hello-world | sc}}
* CapitalCase (chained): {{hello-world | lc | c}}
* Trim: {{hello-world | trim}}
* M.x: {{versions.stack | M.x }}
* M.M: {{versions.stack | M.M }}
* M: {{versions.stack | M }}
* M+1: {{versions.stack | M+1 }}
* M+1 | M.M: {{versions.stack | M+1 | M.M }}
* M.M+1: {{versions.stack | M.M+1 }}
"""

    [<Fact>]
    let ``validate HTML: replace substitution`` () =
        markdown |> convertsToHtml """<ul>
 	<li>Lowercase: hello world!</li>
 	<li>Uppercase: HELLO WORLD!</li>
 	<li>TitleCase: Hello World!</li>
 	<li>kebab-case: hello-world!</li>
 	<li>camelCase: helloWorld!</li>
 	<li>PascalCase: HelloWorld!</li>
 	<li>SnakeCase: hello_world!</li>
 	<li>CapitalCase (chained): Hello world!</li>
 	<li>Trim: Hello world</li>
 	<li>M.x: 9.x</li>
 	<li>M.M: 9.1</li>
 	<li>M: 9</li>
 	<li>M+1: 10.0.0</li>
 	<li>M+1 | M.M: 10.0</li>
 	<li>M.M+1: 9.2.0</li>
 </ul>
        """
