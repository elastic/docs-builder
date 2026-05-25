import { formatBytesString } from '../calculations'
import { formatExactBytes } from '../formatNumbers'
import { EuiText } from '@elastic/eui'
import { CalcToolTip } from './CalcToolTip'

interface ByteSizeValueProps {
    bytes?: number
    bytesMin?: number
    bytesMax?: number
    variant?: 'default' | 'hero'
    /** Override the main label (e.g. hero RAM uses different rounding). */
    primaryLabel?: string
    /** Extra tooltip line (e.g. DiskBBQ range context). */
    tooltipNote?: string
}

function resolveByteBounds(
    bytes: number | undefined,
    bytesMin: number | undefined,
    bytesMax: number | undefined
): { min: number; max: number } {
    if (bytesMin !== undefined && bytesMax !== undefined) {
        return { min: bytesMin, max: bytesMax }
    }
    const value = bytes ?? 0
    return { min: value, max: value }
}

export function ByteSizeValue({
    bytes,
    bytesMin,
    bytesMax,
    variant = 'default',
    primaryLabel,
    tooltipNote,
}: ByteSizeValueProps) {
    const { min, max } = resolveByteBounds(bytes, bytesMin, bytesMax)
    const isRange = min !== max

    const primary =
        primaryLabel ??
        (isRange
            ? `${formatBytesString(min)} – ${formatBytesString(max)}`
            : formatBytesString(min))

    const tooltipContent = (
        <>
            {isRange ? (
                <>
                    <EuiText size="xs" color="inherit">
                        Min: {formatExactBytes(min)}
                    </EuiText>
                    <EuiText size="xs" color="inherit">
                        Max: {formatExactBytes(max)}
                    </EuiText>
                </>
            ) : (
                <EuiText size="xs" color="inherit">
                    {formatExactBytes(min)}
                </EuiText>
            )}
            {tooltipNote && (
                <EuiText size="xs" color="subdued">
                    {tooltipNote}
                </EuiText>
            )}
            <EuiText size="xs" color="subdued">
                Binary prefixes (1 GB = 1,024 MB)
            </EuiText>
        </>
    )

    const isHero = variant === 'hero'

    return (
        <CalcToolTip
            content={tooltipContent}
            position="top"
            repositionOnScroll
            rowClassName={
                isHero
                    ? 'vectorSizingCalc__byteSizeRow--hero'
                    : 'vectorSizingCalc__byteSizeRow--kv'
            }
            wrapperClassName={
                isHero
                    ? 'vectorSizingCalc__byteSizeValue vectorSizingCalc__byteSizeValue--hero'
                    : 'vectorSizingCalc__byteSizeValue'
            }
        >
            <span className="vectorSizingCalc__byteSizeValuePrimary">
                {primary}
            </span>
        </CalcToolTip>
    )
}
