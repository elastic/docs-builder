module ``block elements``.``lists``

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
                    <strong>Consumption-based billing</strong>:</p>
                <p>You pay for the actual product used, regardless of the application or use case. This is different from subscription-based billing models where customers pay a flat fee restricted by usage quotas, or one-time upfront payment billing models such as those used for on-prem software licenses.</p>
                <p>You can purchase credits for a single or multi-year contract. Consumption is on demand, and every month we deduct from your balance based on your usage and contract terms. This allows you to seamlessly expand your usage to the full extent of your requirements and available budget, without any quotas or restrictions.</p>
            </li>
        </ul>
        """

