import { formatHeroSizeParts } from '../calculations'

interface HeroSizeLineProps {
    bytes?: number
    bytesMin?: number
    bytesMax?: number
    resourceLabel: 'Disk' | 'RAM'
}

export function HeroSizeLine({
    bytes = 0,
    bytesMin,
    bytesMax,
    resourceLabel,
}: HeroSizeLineProps) {
    const { value, unit } = formatHeroSizeParts(bytes, bytesMin, bytesMax)

    return (
        <div className="vectorSizingCalc__heroSizeColumn">
            <span className="vectorSizingCalc__heroSizeValue">{value}</span>
            <span className="vectorSizingCalc__heroSizeUnit">
                {unit} {resourceLabel}
            </span>
        </div>
    )
}
