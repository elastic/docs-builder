import '../../eui-icons-cache'
import { DiagnosticsFooterBar } from './DiagnosticsFooterBar'
import { ResizableDiagnosticsHud } from './ResizableDiagnosticsHud'
import {
    connectToDiagnosticsStream,
    disconnectFromDiagnosticsStream,
} from './diagnosticsStreamClient'
import { EuiProvider } from '@elastic/eui'
import r2wc from '@r2wc/react-to-web-component'
import * as React from 'react'
import { useEffect } from 'react'

const DiagnosticsPanelInner: React.FC = () => {
    useEffect(() => {
        connectToDiagnosticsStream()
        return () => {
            disconnectFromDiagnosticsStream()
        }
    }, [])

    return (
        <div
            style={{
                position: 'fixed',
                bottom: 0,
                left: 0,
                right: 0,
                zIndex: 9998,
                display: 'flex',
                flexDirection: 'column-reverse',
            }}
        >
            <DiagnosticsFooterBar />
            <ResizableDiagnosticsHud />
        </div>
    )
}

export const DiagnosticsPanel: React.FC = () => (
    <EuiProvider colorMode="dark" globalStyles={false} utilityClasses={false}>
        <DiagnosticsPanelInner />
    </EuiProvider>
)

// Register as web component
customElements.define('diagnostics-panel', r2wc(DiagnosticsPanel))
