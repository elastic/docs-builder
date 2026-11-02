/** AWS bills data transfer and retrieval per GB, where 1 GB is 2^30 bytes. */
export const BYTES_PER_GB = 1024 ** 3

export interface CostInputs {
    /** S3 requests per run: `planning_requests` plus each `ExternalDataSourceOperator` `request_count`. */
    requestsPerRun: number
    /** Top-level `bytes_read` from the ES|QL response. */
    bytesReadPerRun: number
    pricePerThousandRequests: number
    pricePerGbTransferred: number
    pricePerGbRetrieved: number
}

export interface RunFrequency {
    dashboardPanels: number
    refreshesPerHour: number
    concurrentUsers: number
    hoursPerDay: number
    /** Runs per day from alerting rules, scheduled jobs, or other callers. */
    extraRunsPerDay: number
    daysPerMonth: number
}

export interface CostBreakdown {
    requests: number
    transfer: number
    retrieval: number
    total: number
}

export interface CostResult {
    gbReadPerRun: number
    perRun: CostBreakdown
    runsPerDay: number
    runsPerMonth: number
    perDay: number
    perMonth: number
}

export function bytesToGb(bytes: number): number {
    return bytes / BYTES_PER_GB
}

export function runsPerDay(frequency: RunFrequency): number {
    const dashboardRuns =
        frequency.dashboardPanels *
        frequency.refreshesPerHour *
        frequency.concurrentUsers *
        frequency.hoursPerDay
    return dashboardRuns + frequency.extraRunsPerDay
}

/**
 * cost = (requests / 1000 × P_requests + GB read × P_transfer + GB read × P_retrieval) × runs
 */
export function costPerRun(inputs: CostInputs): CostBreakdown {
    const gbRead = bytesToGb(inputs.bytesReadPerRun)
    const requests =
        (inputs.requestsPerRun / 1000) * inputs.pricePerThousandRequests
    const transfer = gbRead * inputs.pricePerGbTransferred
    const retrieval = gbRead * inputs.pricePerGbRetrieved
    return {
        requests,
        transfer,
        retrieval,
        total: requests + transfer + retrieval,
    }
}

export function calculate(
    inputs: CostInputs,
    frequency: RunFrequency
): CostResult {
    const perRun = costPerRun(inputs)
    const daily = runsPerDay(frequency)
    const monthly = daily * frequency.daysPerMonth
    return {
        gbReadPerRun: bytesToGb(inputs.bytesReadPerRun),
        perRun,
        runsPerDay: daily,
        runsPerMonth: monthly,
        perDay: perRun.total * daily,
        perMonth: perRun.total * monthly,
    }
}

/** Parses a non-negative number, accepting comma thousands separators. Returns NaN when invalid. */
export function parseAmount(raw: string): number {
    const trimmed = raw.trim().replace(/,/g, '')
    if (trimmed === '' || !/^\d*\.?\d+$|^\d+\.$/.test(trimmed)) return NaN
    return Number(trimmed)
}

/** Formats a cost with enough precision that small per-run amounts don't round to zero. */
export function formatCost(value: number): string {
    if (value === 0) return '$0.00'
    if (value < 0.01) {
        return `$${value.toLocaleString('en-US', { maximumSignificantDigits: 3 })}`
    }
    return value.toLocaleString('en-US', {
        style: 'currency',
        currency: 'USD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    })
}
