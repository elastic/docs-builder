export const NUMBER_LOCALE = 'en-US' as const

export function formatGroupedInteger(value: number): string {
  return value.toLocaleString(NUMBER_LOCALE, { maximumFractionDigits: 0 })
}

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
