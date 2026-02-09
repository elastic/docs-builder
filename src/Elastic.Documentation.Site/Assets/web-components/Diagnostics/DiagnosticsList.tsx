import { DiagnosticRow } from './DiagnosticRow'
import {
    useDiagnosticsStore,
    DiagnosticItem,
    DiagnosticSeverity,
} from './diagnostics.store'
import { EuiIcon, EuiText, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'
import { useEffect, useRef, useMemo } from 'react'

const SEVERITY_ORDER: Record<DiagnosticSeverity, number> = {
    error: 0,
    warning: 1,
    hint: 2,
}

export const DiagnosticsList: React.FC = () => {
    const { diagnostics, filters, status } = useDiagnosticsStore()
    const { euiTheme } = useEuiTheme()
    const listRef = useRef<HTMLDivElement>(null)
    const prevLength = useRef(diagnostics.length)

    const filteredDiagnostics = useMemo(
        () =>
            diagnostics
                .filter((d: DiagnosticItem) => {
                    if (d.severity === 'error' && !filters.errors) return false
                    if (d.severity === 'warning' && !filters.warnings)
                        return false
                    if (d.severity === 'hint' && !filters.hints) return false
                    return true
                })
                .sort(
                    (a: DiagnosticItem, b: DiagnosticItem) =>
                        SEVERITY_ORDER[a.severity] - SEVERITY_ORDER[b.severity]
                ),
        [diagnostics, filters]
    )

    useEffect(() => {
        if (diagnostics.length > prevLength.current && listRef.current) {
            listRef.current.scrollTop = listRef.current.scrollHeight
        }
        prevLength.current = diagnostics.length
    }, [diagnostics.length])

    const emptyMessage =
        status === 'building'
            ? 'Waiting for diagnostics...'
            : diagnostics.length === 0
              ? 'No messages to display'
              : 'No diagnostics match the current filters'

    return (
        <div
            ref={listRef}
            css={css`
                flex: 1;
                min-height: 0;
                overflow-y: auto;
                display: ${filteredDiagnostics.length === 0 ? 'flex' : 'block'};
                justify-content: center;
                align-items: center;
            `}
        >
            {filteredDiagnostics.length === 0 ? (
                <div
                    css={css`
                        display: flex;
                        flex-direction: column;
                        align-items: center;
                        gap: ${euiTheme.size.s};
                    `}
                >
                    <EuiIcon type="checkCircle" size="l" color="subdued" />
                    <EuiText size="xs" color="subdued">
                        {emptyMessage}
                    </EuiText>
                </div>
            ) : (
                filteredDiagnostics.map((diagnostic: DiagnosticItem) => (
                    <DiagnosticRow
                        key={diagnostic.id}
                        diagnostic={diagnostic}
                    />
                ))
            )}
        </div>
    )
}
