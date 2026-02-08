import { SeverityIndicator } from './SeverityIndicator'
import { useDiagnosticsStore } from './diagnostics.store'
import {
    EuiFlexGroup,
    EuiIcon,
    EuiLoadingSpinner,
    EuiText,
    EuiFlexItem,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'

export const DiagnosticsSummary: React.FC = () => {
    const { euiTheme } = useEuiTheme()
    const { errors, warnings, hints, status, isConnected } =
        useDiagnosticsStore()

    const hasIssues = errors > 0 || warnings > 0 || hints > 0
    const totalIssues = errors + warnings + hints

    if (!isConnected && status === 'idle') {
        return (
            <EuiFlexGroup alignItems="center" gutterSize="xs">
                <EuiFlexItem>
                    <EuiLoadingSpinner size="s" />
                </EuiFlexItem>
                <EuiFlexItem>
                    <EuiText size="xs" color="subdued">
                        Connecting…
                    </EuiText>
                </EuiFlexItem>
            </EuiFlexGroup>
        )
    }

    if (status === 'building' && !hasIssues) {
        return (
            <EuiFlexGroup alignItems="center" gutterSize="xs">
                <EuiLoadingSpinner size="s" />
                <EuiText size="xs">Building...</EuiText>
            </EuiFlexGroup>
        )
    }

    if (hasIssues) {
        return (
            <EuiFlexGroup
                alignItems="center"
                gutterSize="m"
                wrap
                responsive={false}
                css={css`
                    font-family: ${euiTheme.font.familyCode};
                `}
            >
                <EuiFlexItem grow={0}>
                    <EuiFlexGroup
                        alignItems="center"
                        gutterSize="xs"
                        component="span"
                        responsive={false}
                    >
                        <EuiFlexItem grow={0}>
                            <EuiIcon type="dot" color="danger" size="s" />
                        </EuiFlexItem>
                        <EuiFlexItem grow={0}>
                            <EuiText size="xs" color="subdued">
                                {totalIssues} issue
                                {totalIssues !== 1 ? 's' : ''}
                            </EuiText>
                        </EuiFlexItem>
                    </EuiFlexGroup>
                </EuiFlexItem>
                {errors > 0 && (
                    <EuiFlexItem grow={0}>
                        <SeverityIndicator severity="error" count={errors} />
                    </EuiFlexItem>
                )}
                {warnings > 0 && (
                    <EuiFlexItem grow={0}>
                        <SeverityIndicator
                            severity="warning"
                            count={warnings}
                        />
                    </EuiFlexItem>
                )}
                {hints > 0 && (
                    <EuiFlexItem grow={0}>
                        <SeverityIndicator severity="hint" count={hints} />
                    </EuiFlexItem>
                )}
            </EuiFlexGroup>
        )
    }

    return (
        <EuiFlexGroup alignItems="center" gutterSize="xs" component="span">
            <EuiIcon type="dot" color="success" size="s" />
            <EuiText size="xs" color="success">
                All clear
            </EuiText>
        </EuiFlexGroup>
    )
}
