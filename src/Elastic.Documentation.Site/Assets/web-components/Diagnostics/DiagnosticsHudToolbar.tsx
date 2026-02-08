import { useDiagnosticsStore, FilterMode } from './diagnostics.store'
import {
    EuiButtonEmpty,
    EuiButtonIcon,
    EuiIcon,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'

const FILTER_OPTIONS: { mode: FilterMode; label: string }[] = [
    { mode: 'all', label: 'All' },
    { mode: 'errors', label: 'Errors' },
    { mode: 'warnings', label: 'Warnings' },
    { mode: 'hints', label: 'Hints' },
]

function getActiveFilterMode(filters: {
    errors: boolean
    warnings: boolean
    hints: boolean
}): FilterMode {
    if (filters.errors && filters.warnings && filters.hints) return 'all'
    if (filters.errors && !filters.warnings && !filters.hints) return 'errors'
    if (!filters.errors && filters.warnings && !filters.hints) return 'warnings'
    if (!filters.errors && !filters.warnings && filters.hints) return 'hints'
    return 'all'
}

export const DiagnosticsHudToolbar: React.FC = () => {
    const { setHudOpen, filters, setFilterMode } = useDiagnosticsStore()
    const { euiTheme } = useEuiTheme()

    const activeMode = getActiveFilterMode(filters)

    return (
        <div
            style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                borderBottom: `1px solid ${euiTheme.border.color}`,
                paddingBlock: `${euiTheme.size.xs}`,
                paddingInline: `${euiTheme.size.m}`,
                flexShrink: 0,
            }}
        >
            <div
                css={css`
                    display: flex;
                    align-items: center;
                    gap: ${euiTheme.size.s};
                `}
            >
                <EuiIcon type="filter" size="s" color="subdued" />
                {FILTER_OPTIONS.map(({ mode, label }) => (
                    <EuiButtonEmpty
                        key={mode}
                        size="xs"
                        color={activeMode === mode ? 'primary' : 'text'}
                        onClick={() => setFilterMode(mode)}
                        aria-pressed={activeMode === mode}
                        aria-label={`Show ${label}`}
                    >
                        {label}
                    </EuiButtonEmpty>
                ))}
            </div>
            <EuiButtonIcon
                iconType="cross"
                aria-label="Close diagnostics panel"
                onClick={() => setHudOpen(false)}
            />
        </div>
    )
}
