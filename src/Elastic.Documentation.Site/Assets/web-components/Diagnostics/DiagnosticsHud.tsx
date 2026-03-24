import { DiagnosticsHudToolbar } from './DiagnosticsHudToolbar'
import { DiagnosticsList } from './DiagnosticsList'
import { useDiagnosticsStore } from './diagnostics.store'
import { useEuiTheme } from '@elastic/eui'
import * as React from 'react'
import { useEffect } from 'react'

export const DiagnosticsHud: React.FC = () => {
    const { isHudOpen, setHudOpen } = useDiagnosticsStore()
    const { euiTheme } = useEuiTheme()

    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === 'Escape' && isHudOpen) {
                setHudOpen(false)
            }
        }
        document.addEventListener('keydown', handleKeyDown)
        return () => document.removeEventListener('keydown', handleKeyDown)
    }, [isHudOpen, setHudOpen])

    return (
        <div
            style={{
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                minHeight: 0,
                backgroundColor: euiTheme.colors.body,
            }}
        >
            <DiagnosticsHudToolbar />
            <DiagnosticsList />
        </div>
    )
}
