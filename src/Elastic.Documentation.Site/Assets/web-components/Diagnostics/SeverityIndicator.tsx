import { SEVERITY_CONFIG, DiagnosticSeverity } from './diagnostics.store'
import { EuiFlexGroup, EuiIcon, EuiText } from '@elastic/eui'
import * as React from 'react'

interface SeverityIndicatorProps {
    severity: DiagnosticSeverity
    count: number
}

const pluralize = (word: string, count: number) =>
    count === 1 ? word : `${word}s`

export const SeverityIndicator: React.FC<SeverityIndicatorProps> = ({
    severity,
    count,
}) => {
    const { iconType, color } = SEVERITY_CONFIG[severity]

    return (
        <EuiFlexGroup alignItems="center" gutterSize="xs" component="span">
            <EuiIcon type={iconType} color={color} size="s" />
            <EuiText size="xs" color={color}>
                {count} {pluralize(severity, count)}
            </EuiText>
        </EuiFlexGroup>
    )
}
