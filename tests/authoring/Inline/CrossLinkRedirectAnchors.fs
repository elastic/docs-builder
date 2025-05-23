// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``inline elements``.``complex anchors``

open Xunit
open authoring.CrossLinkResolverAssertions

type ``scenario 1``() =

    [<Fact>]
    let ``No anchor redirects to new-anchorless page``() =
        resolvesTo ("docs-content://testing/redirects/multi-topic-page-1-old.md") "/testing/redirects/multi-topic-page-1-new-anchorless"

    [<Fact>]
    let ``Unmatched anchor for '!' rule redirects to new-anchorless page and drops anchor``() =
        resolvesTo ("docs-content://testing/redirects/multi-topic-page-1-old.md#unmatched-anchor") "/testing/redirects/multi-topic-page-1-new-anchorless"

    [<Fact>]
    let ``topic-a-intro redirects to topic-a-subpage and drops anchor (null target)``() =
        resolvesTo ("docs-content://testing/redirects/multi-topic-page-1-old.md#topic-a-intro") "/testing/redirects/multi-topic-page-1-new-topic-a-subpage"

    [<Fact>]
    let ``topic-a-details redirects to topic-a-subpage with new anchor``() =
        resolvesTo ("docs-content://testing/redirects/multi-topic-page-1-old.md#topic-a-details") "/testing/redirects/multi-topic-page-1-new-topic-a-subpage#details-anchor"

    [<Fact>]
    let ``topic-b-main redirects to topic-b-subpage with new anchor``() =
        resolvesTo ("docs-content://testing/redirects/multi-topic-page-1-old.md#topic-b-main") "/testing/redirects/multi-topic-page-1-new-topic-b-subpage#main-anchor"

    [<Fact>]
    let ``topic-c-main redirects to old page and keeps anchor``() =
        resolvesTo ("docs-content://testing/redirects/multi-topic-page-1-old.md#topic-c-main") "/testing/redirects/multi-topic-page-1-old#topic-c-main"


type ``Scenario 2``() =

    [<Fact>]
    let ``No anchor redirects to old page (self)``() =
        resolvesTo ("docs-content://testing/redirects/multi-topic-page-2-old.md") "/testing/redirects/multi-topic-page-2-old"

    [<Fact>]
    let ``Unmatched anchor for '{}' rule redirects to old page (self) and keeps anchor``() =
        resolvesTo ("docs-content://testing/redirects/multi-topic-page-2-old.md#unmatched-anchor") "/testing/redirects/multi-topic-page-2-old#unmatched-anchor"

    [<Fact>]
    let ``topic-a-intro redirects to topic-a-subpage with new anchor``() =
        resolvesTo ("docs-content://testing/redirects/multi-topic-page-2-old.md#topic-a-intro") "/testing/redirects/multi-topic-page-2-new-topic-a-subpage#introduction"

    [<Fact>]
    let ``topic-a-details redirects to topic-a-subpage and drops anchor (null target)``() =
        resolvesTo ("docs-content://testing/redirects/multi-topic-page-2-old.md#topic-a-details") "/testing/redirects/multi-topic-page-2-new-topic-a-subpage"

    [<Fact>]
    let ``topic-b-main redirects to topic-b-subpage with new anchor``() =
        resolvesTo ("docs-content://testing/redirects/multi-topic-page-2-old.md#topic-b-main") "/testing/redirects/multi-topic-page-2-new-topic-b-subpage#summary"

    [<Fact>]
    let ``topic-b-config redirects to topic-b-subpage and drops anchor (null target)``() =
        resolvesTo ("docs-content://testing/redirects/multi-topic-page-2-old.md#topic-b-config") "/testing/redirects/multi-topic-page-2-new-topic-b-subpage"