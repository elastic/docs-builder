import {
    useDiagnosticsStore,
    DiagnosticItem,
    BuildStatus,
} from './diagnostics.store'
import { fetchEventSource } from '@microsoft/fetch-event-source'

interface DiagnosticData {
    severity: string
    file: string
    message: string
    line?: number
    column?: number
}

interface BuildEvent {
    type: string
    timestamp: number
    diagnostic?: DiagnosticData
    diagnostics?: DiagnosticData[]
    errors?: number
    warnings?: number
    hints?: number
    status?: string
}

let abortController: AbortController | null = null
let diagnosticIdCounter = 0

export function connectToDiagnosticsStream(): void {
    // Disconnect any existing connection
    disconnectFromDiagnosticsStream()

    abortController = new AbortController()
    const store = useDiagnosticsStore.getState()

    fetchEventSource('/_api/diagnostics/stream', {
        signal: abortController.signal,

        onopen: async (response) => {
            if (response.ok) {
                store.setConnected(true)
            } else {
                console.error(
                    '[Diagnostics] SSE connection failed:',
                    response.status
                )
                store.setConnected(false)
            }
        },

        onmessage: (event) => {
            try {
                const data: BuildEvent = JSON.parse(event.data)
                handleBuildEvent(data)
            } catch (e) {
                console.error('[Diagnostics] Failed to parse event:', e)
            }
        },

        onerror: (err) => {
            console.error('[Diagnostics] SSE error:', err)
            store.setConnected(false)

            // Retry connection after a delay
            setTimeout(() => {
                if (abortController && !abortController.signal.aborted) {
                    connectToDiagnosticsStream()
                }
            }, 3000)
        },

        onclose: () => {
            store.setConnected(false)
        },
    }).catch((err) => {
        if (err.name !== 'AbortError') {
            console.error('[Diagnostics] SSE fetch error:', err)
        }
    })
}

export function disconnectFromDiagnosticsStream(): void {
    if (abortController) {
        abortController.abort()
        abortController = null
    }
    useDiagnosticsStore.getState().setConnected(false)
}

function handleBuildEvent(event: BuildEvent): void {
    const store = useDiagnosticsStore.getState()

    switch (event.type) {
        case 'state':
            // Initial state from server - includes status, counts, and historical diagnostics
            store.setCounts(
                event.errors ?? 0,
                event.warnings ?? 0,
                event.hints ?? 0
            )
            if (event.status) {
                store.setStatus(event.status as BuildStatus)
            }
            // Load any stored diagnostics from previous builds
            if (event.diagnostics && event.diagnostics.length > 0) {
                store.clearDiagnostics()
                // Restore counts since clearDiagnostics resets them
                store.setCounts(
                    event.errors ?? 0,
                    event.warnings ?? 0,
                    event.hints ?? 0
                )
                event.diagnostics.forEach((diag) => {
                    const diagnostic: DiagnosticItem = {
                        id: `diag-${++diagnosticIdCounter}`,
                        severity: diag.severity as DiagnosticItem['severity'],
                        file: diag.file,
                        message: diag.message,
                        line: diag.line,
                        column: diag.column,
                        timestamp: event.timestamp,
                    }
                    store.addDiagnostic(diagnostic)
                })
            }
            break

        case 'build_start':
            store.setStatus('building')
            store.clearDiagnostics()
            diagnosticIdCounter = 0
            break

        case 'build_complete':
            store.setStatus('complete')
            store.setCounts(
                event.errors ?? 0,
                event.warnings ?? 0,
                event.hints ?? 0
            )
            break

        case 'build_cancelled':
            store.setStatus('idle')
            break

        case 'diagnostic':
            if (event.diagnostic) {
                const diagnostic: DiagnosticItem = {
                    id: `diag-${++diagnosticIdCounter}`,
                    severity: event.diagnostic
                        .severity as DiagnosticItem['severity'],
                    file: event.diagnostic.file,
                    message: event.diagnostic.message,
                    line: event.diagnostic.line,
                    column: event.diagnostic.column,
                    timestamp: event.timestamp,
                }
                store.addDiagnostic(diagnostic)

                // Update counts based on severity
                const currentState = useDiagnosticsStore.getState()
                if (diagnostic.severity === 'error') {
                    store.setCounts(
                        currentState.errors + 1,
                        currentState.warnings,
                        currentState.hints
                    )
                } else if (diagnostic.severity === 'warning') {
                    store.setCounts(
                        currentState.errors,
                        currentState.warnings + 1,
                        currentState.hints
                    )
                } else if (diagnostic.severity === 'hint') {
                    store.setCounts(
                        currentState.errors,
                        currentState.warnings,
                        currentState.hints + 1
                    )
                }
            }
            break

        default:
            console.warn('[Diagnostics] Unknown event type:', event.type)
    }
}
