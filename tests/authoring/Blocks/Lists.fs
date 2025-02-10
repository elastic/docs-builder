// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
module ``block elements``.``list elements``

open Xunit
open authoring

type ``supports loose lists`` () =
    static let markdown = Setup.Markdown """
* **Consumption-based billing**:

   You pay for the actual product used, regardless of the application or use case. This is different from subscription-based billing models where customers pay a flat fee restricted by usage quotas, or one-time upfront payment billing models such as those used for on-prem software licenses.

   You can purchase credits for a single or multi-year contract. Consumption is on demand, and every month we deduct from your balance based on your usage and contract terms. This allows you to seamlessly expand your usage to the full extent of your requirements and available budget, without any quotas or restrictions.
"""

    [<Fact>]
    let ``validate HTML: adds paragraphs`` () =
        markdown |> convertsToHtml """
        <ul>
            <li>
                <p>
                    <strong>onsumption-based billing</strong>:</p>
                <p>You pay for the actual product used, regardless of the application or use case. This is different from subscription-based billing models where customers pay a flat fee restricted by usage quotas, or one-time upfront payment billing models such as those used for on-prem software licenses.</p>
                <p>You can purchase credits for a single or multi-year contract. Consumption is on demand, and every month we deduct from your balance based on your usage and contract terms. This allows you to seamlessly expand your usage to the full extent of your requirements and available budget, without any quotas or restrictions.</p>
            </li>
        </ul>
        """

