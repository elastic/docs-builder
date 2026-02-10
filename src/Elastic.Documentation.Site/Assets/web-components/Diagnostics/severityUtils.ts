/** Priority: error > warning > hint. Returns theme color key or null when no issues. */
export function getSeverityColor(
    errors: number,
    warnings: number,
    hints: number
): 'danger' | 'warning' | 'primary' | null {
    if (errors > 0) return 'danger'
    if (warnings > 0) return 'warning'
    if (hints > 0) return 'primary'
    return null
}
