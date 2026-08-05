import { formatHeroSizeParts } from '../calculations'

interface HeroSizeLineProps {
    bytes?: number
    resourceLabel: 'Disk' | 'RAM'
}

export function HeroSizeLine({ bytes = 0, resourceLabel }: HeroSizeLineProps) {
    const { value, unit } = formatHeroSizeParts(bytes)

    return (
        <div className="vectorSizingCalc__heroSizeColumn">
            <span className="vectorSizingCalc__heroSizeValue">{value}</span>
            <span className="vectorSizingCalc__heroSizeUnit">
                {unit} {resourceLabel}
            </span>
        </div>
    )
}
