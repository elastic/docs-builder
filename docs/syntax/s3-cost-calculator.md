# Amazon S3 cost calculator

The `{s3-cost-calculator}` directive embeds an interactive calculator that estimates the AWS charges for an ES|QL Data Federation query that reads from Amazon S3. The docs site loads the React/EUI web component that registers the `<s3-cost-calculator>` custom element.

## Usage

```markdown
:::{s3-cost-calculator}
:::
```

No body content or options are required. Readers enter their values in the widget.

## Live preview

:::{s3-cost-calculator}
:::

## What the calculator does

Readers enter values from an ES|QL query profile, their own AWS prices, and how often the query runs. The calculator doesn't include any AWS prices, because prices change and vary by region and storage class.

Inputs:

- **S3 requests per run**: `planning_requests` plus the `request_count` of each `ExternalDataSourceOperator` in the query profile.
- **Bytes read per run**: the top-level `bytes_read` value in the ES|QL response.
- **Prices**: price per 1,000 requests, price per GB transferred, and an optional retrieval price per GB for infrequent-access storage classes. Transfer defaults to 0, because reads in the same AWS region are free.
- **Run frequency**: dashboard panels × refreshes per hour × concurrent users × hours per day, plus other runs per day from alerting rules or scheduled jobs, and days per month.

The calculator shows the cost per run, per day, and per month, using this formula:

```{math}
\text{cost} = \left( \frac{\text{requests}}{1000} \times P_{\text{requests}} + \text{GB read} \times (P_{\text{transfer}} + P_{\text{retrieval}}) \right) \times \text{runs}
```

GB read is `bytes_read` divided by 1,073,741,824 (1,024³), which is how AWS counts a GB for billing.

## Requirements

This directive only produces useful output where the assembled documentation site includes the S3 cost calculator bundle (see `src/Elastic.Documentation.Site/Assets/web-components/S3CostCalculator/`).
