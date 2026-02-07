import { DiagnosticsButton } from './DiagnosticsButton'
import { ResizableDiagnosticsHud } from './ResizableDiagnosticsHud'
import {
    connectToDiagnosticsStream,
    disconnectFromDiagnosticsStream,
} from './diagnosticsStreamClient'
import r2wc from '@r2wc/react-to-web-component'
import * as React from 'react'
import { useEffect } from 'react'

export const DiagnosticsPanel: React.FC = () => {
    useEffect(() => {
        // Connect to SSE stream on mount
        connectToDiagnosticsStream()

        // Disconnect on unmount
        return () => {
            disconnectFromDiagnosticsStream()
        }
    }, [])

    return (
        <>
            <DiagnosticsButton />
            <ResizableDiagnosticsHud />
        </>
    )
}

// Register as web component
customElements.define('diagnostics-panel', r2wc(DiagnosticsPanel))
