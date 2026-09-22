import {
    useDiagnosticsStore,
    DiagnosticItem,
    BuildStatus,
} from './diagnostics.store'

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

const STATE_URL = '/_api/diagnostics/state'
const POLL_MS = 1500

let closed = true
let connectionGeneration = 0
let pollTimer: ReturnType<typeof setInterval> | null = null
let pollAbort: AbortController | null = null
let diagnosticIdCounter = 0

function afterDocumentLoad(callback: () => void): void {
    const run = () => window.setTimeout(callback, 0)
    if (document.readyState === 'complete') {
        run()
        return
    }
    window.addEventListener('load', run, { once: true })
}

export function connectToDiagnosticsStream(): void {
    disconnectFromDiagnosticsStream()
    closed = false
    const generation = ++connectionGeneration
    afterDocumentLoad(() => {
        if (closed || generation !== connectionGeneration) return
        void pollDiagnosticsState()
        pollTimer = setInterval(() => {
            void pollDiagnosticsState()
        }, POLL_MS)
    })
}

export function disconnectFromDiagnosticsStream(): void {
    closed = true
    if (pollTimer !== null) {
        clearInterval(pollTimer)
        pollTimer = null
    }
    pollAbort?.abort()
    pollAbort = null
    useDiagnosticsStore.getState().setConnected(false)
}

async function pollDiagnosticsState(): Promise<void> {
    if (pollAbort) return
    const abort = new AbortController()
    pollAbort = abort
    try {
        const response = await fetch(STATE_URL, {
            signal: abort.signal,
            cache: 'no-store',
        })
        if (closed) return
        if (!response.ok) {
            useDiagnosticsStore.getState().setConnected(false)
            return
        }
        const data = (await response.json()) as BuildEvent
        if (closed) return
        useDiagnosticsStore.getState().setConnected(true)
        handleBuildEvent(data)
    } catch (err) {
        if (closed || (err instanceof Error && err.name === 'AbortError'))
            return
        useDiagnosticsStore.getState().setConnected(false)
    } finally {
        if (pollAbort === abort) pollAbort = null
    }
}

function handleBuildEvent(event: BuildEvent): void {
    const store = useDiagnosticsStore.getState()

    switch (event.type) {
        case 'state':
            applyStateSnapshot(store, event)
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

function applyStateSnapshot(
    store: ReturnType<typeof useDiagnosticsStore.getState>,
    event: BuildEvent
): void {
    const errors = event.errors ?? 0
    const warnings = event.warnings ?? 0
    const hints = event.hints ?? 0
    if (event.status) store.setStatus(event.status as BuildStatus)
    store.clearDiagnostics()
    diagnosticIdCounter = 0
    store.setCounts(errors, warnings, hints)
    for (const diag of event.diagnostics ?? []) {
        store.addDiagnostic({
            id: `diag-${++diagnosticIdCounter}`,
            severity: diag.severity as DiagnosticItem['severity'],
            file: diag.file,
            message: diag.message,
            line: diag.line,
            column: diag.column,
            timestamp: event.timestamp,
        })
    }
}
