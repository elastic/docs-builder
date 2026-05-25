import { formatBytes } from '../calculations'

interface HeroSizeLineProps {
    bytes: number
    resourceLabel: 'Disk' | 'RAM'
}

export function HeroSizeLine({ bytes, resourceLabel }: HeroSizeLineProps) {
    const { value, unit } = formatBytes(bytes)

    return (
        <div className="vectorSizingCalc__heroSizeColumn">
            <span className="vectorSizingCalc__heroSizeValue">{value}</span>
            <span className="vectorSizingCalc__heroSizeUnit">
                {unit} {resourceLabel}
            </span>
        </div>
    )
}
