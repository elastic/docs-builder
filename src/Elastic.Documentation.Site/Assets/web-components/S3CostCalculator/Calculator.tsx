import { calculate, formatCost, parseAmount } from './calculations'
import {
    EuiFieldText,
    EuiFormRow,
    EuiSpacer,
    EuiStat,
    EuiText,
    EuiTitle,
} from '@elastic/eui'
import { useMemo, useState } from 'react'

type FieldKey =
    | 'requestsPerRun'
    | 'bytesReadPerRun'
    | 'pricePerThousandRequests'
    | 'pricePerGbTransferred'
    | 'pricePerGbRetrieved'
    | 'dashboardPanels'
    | 'refreshesPerHour'
    | 'concurrentUsers'
    | 'hoursPerDay'
    | 'extraRunsPerDay'
    | 'daysPerMonth'

interface FieldConfig {
    key: FieldKey
    label: string
    help: string
    placeholder?: string
}

const QUERY_FIELDS: FieldConfig[] = [
    {
        key: 'requestsPerRun',
        label: 'S3 requests per run',
        help: 'Add planning_requests and the request_count of each ExternalDataSourceOperator in the query profile.',
    },
    {
        key: 'bytesReadPerRun',
        label: 'Bytes read per run',
        help: 'The top-level bytes_read value in the ES|QL response.',
    },
]

const PRICE_FIELDS: FieldConfig[] = [
    {
        key: 'pricePerThousandRequests',
        label: 'Price per 1,000 requests (USD)',
        help: 'The GET request price for your storage class and region, from the Amazon S3 pricing page.',
        placeholder: 'Required',
    },
    {
        key: 'pricePerGbTransferred',
        label: 'Price per GB transferred (USD)',
        help: 'Leave at 0 when the cluster runs in the same AWS region as the bucket. Transfer within a region is free.',
    },
    {
        key: 'pricePerGbRetrieved',
        label: 'Retrieval price per GB (USD)',
        help: 'Only for S3 Standard-IA, S3 One Zone-IA, and S3 Glacier Instant Retrieval. Leave at 0 otherwise.',
    },
]

const FREQUENCY_FIELDS: FieldConfig[] = [
    {
        key: 'dashboardPanels',
        label: 'Dashboard panels',
        help: 'Panels that run this query. Use 0 if no dashboard runs it.',
    },
    {
        key: 'refreshesPerHour',
        label: 'Refreshes per hour',
        help: 'For example, a 5-minute refresh interval is 12 refreshes per hour.',
    },
    {
        key: 'concurrentUsers',
        label: 'Concurrent users',
        help: 'Each user with the dashboard open runs its queries on every refresh.',
    },
    {
        key: 'hoursPerDay',
        label: 'Hours open per day',
        help: 'How long the dashboard stays open each day.',
    },
    {
        key: 'extraRunsPerDay',
        label: 'Other runs per day',
        help: 'Runs from alerting rules, scheduled jobs, or other callers. A rule that checks every minute adds 1,440 runs per day.',
    },
    {
        key: 'daysPerMonth',
        label: 'Days per month',
        help: 'Days the query runs each month.',
    },
]

const DEFAULTS: Record<FieldKey, string> = {
    requestsPerRun: '',
    bytesReadPerRun: '',
    pricePerThousandRequests: '',
    pricePerGbTransferred: '0',
    pricePerGbRetrieved: '0',
    dashboardPanels: '1',
    refreshesPerHour: '12',
    concurrentUsers: '1',
    hoursPerDay: '8',
    extraRunsPerDay: '0',
    daysPerMonth: '30',
}

function formatCount(value: number): string {
    return value.toLocaleString('en-US', { maximumFractionDigits: 2 })
}

export function Calculator() {
    const [values, setValues] = useState(DEFAULTS)

    const parsed = useMemo(
        () =>
            Object.fromEntries(
                Object.entries(values).map(([key, raw]) => [
                    key,
                    parseAmount(raw),
                ])
            ) as Record<FieldKey, number>,
        [values]
    )

    const result = useMemo(() => {
        if (Object.values(parsed).some(Number.isNaN)) return null
        return calculate(parsed, parsed)
    }, [parsed])

    const renderField = (field: FieldConfig) => {
        const raw = values[field.key]
        const isInvalid = raw.trim() !== '' && Number.isNaN(parsed[field.key])
        return (
            <EuiFormRow
                key={field.key}
                label={field.label}
                helpText={field.help}
                isInvalid={isInvalid}
                error={isInvalid ? 'Enter a number of 0 or more.' : undefined}
                fullWidth
            >
                <EuiFieldText
                    value={raw}
                    inputMode="decimal"
                    placeholder={field.placeholder}
                    isInvalid={isInvalid}
                    fullWidth
                    onChange={(e) =>
                        setValues((prev) => ({
                            ...prev,
                            [field.key]: e.target.value,
                        }))
                    }
                />
            </EuiFormRow>
        )
    }

    const section = (title: string, fields: FieldConfig[]) => (
        <>
            <EuiTitle size="xs">
                <h3>{title}</h3>
            </EuiTitle>
            <EuiSpacer size="s" />
            {fields.map(renderField)}
            <EuiSpacer size="l" />
        </>
    )

    return (
        <div className="s3CostCalc">
            <div className="s3CostCalc__grid">
                <div className="s3CostCalc__panel">
                    {section('Query profile', QUERY_FIELDS)}
                    {section('Prices', PRICE_FIELDS)}
                    {section('How often the query runs', FREQUENCY_FIELDS)}
                </div>
                <div
                    className="s3CostCalc__panel s3CostCalc__panel--results"
                    aria-live="polite"
                >
                    <EuiTitle size="xs">
                        <h3>Estimated Amazon S3 cost</h3>
                    </EuiTitle>
                    <EuiSpacer size="m" />
                    {result ? (
                        <>
                            <EuiStat
                                title={formatCost(result.perRun.total)}
                                description="Per run"
                                titleSize="m"
                            />
                            <EuiSpacer size="m" />
                            <EuiStat
                                title={formatCost(result.perDay)}
                                description={`Per day (${formatCount(result.runsPerDay)} runs)`}
                                titleSize="m"
                            />
                            <EuiSpacer size="m" />
                            <EuiStat
                                title={formatCost(result.perMonth)}
                                description={`Per month (${formatCount(result.runsPerMonth)} runs)`}
                                titleSize="m"
                            />
                            <EuiSpacer size="l" />
                            <EuiText size="s">
                                <p>Cost per run:</p>
                                <ul>
                                    <li>
                                        Requests:{' '}
                                        {formatCost(result.perRun.requests)}
                                    </li>
                                    <li>
                                        Transfer of{' '}
                                        {formatCount(result.gbReadPerRun)} GB:{' '}
                                        {formatCost(result.perRun.transfer)}
                                    </li>
                                    <li>
                                        Retrieval:{' '}
                                        {formatCost(result.perRun.retrieval)}
                                    </li>
                                </ul>
                                <p>
                                    This estimate covers AWS charges only. It
                                    doesn&apos;t include AWS KMS requests, NAT
                                    gateway processing, or Elastic costs.
                                </p>
                            </EuiText>
                        </>
                    ) : (
                        <EuiText size="s" color="subdued">
                            <p>
                                Enter the requests and bytes read from your
                                query profile and your price per 1,000 requests
                                to see an estimate.
                            </p>
                        </EuiText>
                    )}
                </div>
            </div>
        </div>
    )
}
