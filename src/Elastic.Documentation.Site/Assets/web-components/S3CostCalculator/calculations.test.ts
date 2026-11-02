import {
    BYTES_PER_GB,
    calculate,
    costPerRun,
    formatCost,
    parseAmount,
    runsPerDay,
    type CostInputs,
    type RunFrequency,
} from './calculations'

const inputs: CostInputs = {
    requestsPerRun: 2_000,
    bytesReadPerRun: 10 * BYTES_PER_GB,
    pricePerThousandRequests: 0.5,
    pricePerGbTransferred: 0.02,
    pricePerGbRetrieved: 0,
}

const frequency: RunFrequency = {
    dashboardPanels: 4,
    refreshesPerHour: 12,
    concurrentUsers: 5,
    hoursPerDay: 8,
    extraRunsPerDay: 40,
    daysPerMonth: 30,
}

describe('costPerRun', () => {
    it('adds request and transfer costs', () => {
        const cost = costPerRun(inputs)
        expect(cost.requests).toBeCloseTo(1)
        expect(cost.transfer).toBeCloseTo(0.2)
        expect(cost.retrieval).toBe(0)
        expect(cost.total).toBeCloseTo(1.2)
    })

    it('adds retrieval fees for infrequent-access storage classes', () => {
        const cost = costPerRun({ ...inputs, pricePerGbRetrieved: 0.01 })
        expect(cost.retrieval).toBeCloseTo(0.1)
        expect(cost.total).toBeCloseTo(1.3)
    })

    it('charges only for requests when transfer is free', () => {
        const cost = costPerRun({ ...inputs, pricePerGbTransferred: 0 })
        expect(cost.total).toBeCloseTo(1)
    })
})

describe('runsPerDay', () => {
    it('multiplies dashboard refreshes and adds extra runs', () => {
        expect(runsPerDay(frequency)).toBe(4 * 12 * 5 * 8 + 40)
    })

    it('counts only extra runs when no dashboard runs the query', () => {
        expect(runsPerDay({ ...frequency, dashboardPanels: 0 })).toBe(40)
    })
})

describe('calculate', () => {
    it('scales the cost per run by daily and monthly runs', () => {
        const result = calculate(inputs, frequency)
        expect(result.gbReadPerRun).toBe(10)
        expect(result.runsPerDay).toBe(1_960)
        expect(result.runsPerMonth).toBe(58_800)
        expect(result.perDay).toBeCloseTo(2_352)
        expect(result.perMonth).toBeCloseTo(70_560)
    })
})

describe('parseAmount', () => {
    it('accepts plain numbers, decimals, and comma separators', () => {
        expect(parseAmount('7340032')).toBe(7_340_032)
        expect(parseAmount('7,340,032')).toBe(7_340_032)
        expect(parseAmount('0.0004')).toBe(0.0004)
        expect(parseAmount('.5')).toBe(0.5)
    })

    it('rejects empty, negative, and non-numeric input', () => {
        expect(parseAmount('')).toBeNaN()
        expect(parseAmount('-1')).toBeNaN()
        expect(parseAmount('abc')).toBeNaN()
    })
})

describe('formatCost', () => {
    it('formats dollars with two decimals', () => {
        expect(formatCost(70_560)).toBe('$70,560.00')
        expect(formatCost(0)).toBe('$0.00')
    })

    it('keeps significant digits for sub-cent amounts', () => {
        expect(formatCost(0.000123)).toBe('$0.000123')
    })
})
