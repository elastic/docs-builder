export const NUMBER_LOCALE = 'en-US' as const

/** Integer with comma thousands separators and dot decimals (en-US). */
export function formatGroupedInteger(value: number): string {
    return value.toLocaleString(NUMBER_LOCALE, { maximumFractionDigits: 0 })
}

/** Exact byte count for secondary display (e.g. under rounded GB/MB). */
export function formatExactBytes(bytes: number): string {
    return `${formatGroupedInteger(bytes)} bytes`
}

/** Normalize user input: commas = thousands, dot = decimal; accepts legacy dot-grouping. */
export function normalizeGroupedNumberInput(raw: string): string {
    const trimmed = raw.trim()
    if (!trimmed) return ''

    if (trimmed.includes(',')) {
        return trimmed.replace(/,/g, '')
    }

    if (/^\d{1,3}(\.\d{3})+$/.test(trimmed)) {
        return trimmed.replace(/\./g, '')
    }

    return trimmed
}
