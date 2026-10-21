// The developer diagnostics console: polls the build state of `docs-builder serve` and lists what it finds.
// The server renders the panel hidden, because without a running build it has nothing to show.
const STATE_URL = '/_api/diagnostics/state'
const POLL_MS = 1500
const OPEN_KEY = 'diagnostics-hud-open'
const MIN_HEIGHT = 125
const DEFAULT_HEIGHT = 200
const KEYBOARD_STEP = 10

export type Severity = 'error' | 'warning' | 'hint'
export type Filter = 'all' | Severity

export interface Diagnostic {
    severity: Severity
    file: string
    message: string
    line?: number
    column?: number
}

export interface BuildEvent {
    type: string
    errors?: number
    warnings?: number
    hints?: number
    status?: string
    diagnostic?: Diagnostic
    diagnostics?: Diagnostic[]
}

export interface DiagnosticsState {
    status: string
    errors: number
    warnings: number
    hints: number
    diagnostics: Diagnostic[]
    connected: boolean
}

const SEVERITY_ORDER: Record<Severity, number> = {
    error: 0,
    warning: 1,
    hint: 2,
}

export function emptyState(): DiagnosticsState {
    return {
        status: 'idle',
        errors: 0,
        warnings: 0,
        hints: 0,
        diagnostics: [],
        connected: false,
    }
}

const countKey = {
    error: 'errors',
    warning: 'warnings',
    hint: 'hints',
} as const

export function applyEvent(
    state: DiagnosticsState,
    event: BuildEvent
): DiagnosticsState {
    switch (event.type) {
        case 'state':
            return {
                ...state,
                status: event.status ?? state.status,
                errors: event.errors ?? 0,
                warnings: event.warnings ?? 0,
                hints: event.hints ?? 0,
                diagnostics: [...(event.diagnostics ?? [])],
            }
        case 'build_start':
            return {
                ...state,
                status: 'building',
                errors: 0,
                warnings: 0,
                hints: 0,
                diagnostics: [],
            }
        case 'build_complete':
            return {
                ...state,
                status: 'complete',
                errors: event.errors ?? 0,
                warnings: event.warnings ?? 0,
                hints: event.hints ?? 0,
            }
        case 'build_cancelled':
            return { ...state, status: 'idle' }
        case 'diagnostic': {
            const diagnostic = event.diagnostic
            if (!diagnostic) return state
            const key = countKey[diagnostic.severity]
            return {
                ...state,
                diagnostics: [...state.diagnostics, diagnostic],
                ...(key ? { [key]: state[key] + 1 } : {}),
            }
        }
        default:
            return state
    }
}

export function visibleDiagnostics(
    diagnostics: Diagnostic[],
    filter: Filter
): Diagnostic[] {
    return diagnostics
        .filter((d) => filter === 'all' || d.severity === filter)
        .sort((a, b) => SEVERITY_ORDER[a.severity] - SEVERITY_ORDER[b.severity])
}

export function worstSeverity(state: DiagnosticsState): Severity | null {
    if (state.errors > 0) return 'error'
    if (state.warnings > 0) return 'warning'
    if (state.hints > 0) return 'hint'
    return null
}

export function locationOf(d: Diagnostic): string {
    let location = d.file
    if (d.line != null) location += `:${d.line}`
    if (d.column != null) location += `:${d.column}`
    return location
}

export function clipboardTextOf(d: Diagnostic): string {
    return `[${d.severity.toUpperCase()}] ${locationOf(d)}: ${d.message}`
}

export function initDiagnostics(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>(
            '[data-diagnostics]:not([data-diagnostics-bound])'
        )
        .forEach((root) => {
            root.dataset.diagnosticsBound = ''
            bindPanel(root)
            root.hidden = false
        })
}

function readOpen(): boolean {
    try {
        return sessionStorage.getItem(OPEN_KEY) === 'true'
    } catch {
        return false
    }
}

function writeOpen(open: boolean) {
    try {
        sessionStorage.setItem(OPEN_KEY, String(open))
    } catch {
        // Storage can be unavailable; the panel just forgets its state.
    }
}

function bindPanel(root: HTMLElement) {
    const q = <T extends HTMLElement>(selector: string) =>
        root.querySelector<T>(selector)!
    const hud = q('[data-diagnostics-hud]')
    const toggle = q<HTMLButtonElement>('[data-diagnostics-toggle]')
    const list = q<HTMLOListElement>('[data-diagnostics-list]')
    const template = q<HTMLTemplateElement>('[data-diagnostics-row]')
    const resize = q('[data-diagnostics-resize]')

    let state = emptyState()
    // Once a poll has succeeded, a later failure means the connection was lost, not that it is still being made.
    let hasConnected = false
    let filter: Filter = 'all'
    let height = DEFAULT_HEIGHT

    function setOpen(open: boolean) {
        hud.hidden = !open
        toggle.setAttribute('aria-expanded', String(open))
        q('[data-diagnostics-action]').textContent = open
            ? 'Hide Console'
            : 'Show Console'
        writeOpen(open)
    }

    function renderSummary() {
        const summary = q('[data-diagnostics-summary]')
        const total = state.errors + state.warnings + state.hints
        const worst = worstSeverity(state)
        root.dataset.severity = worst ?? ''
        summary.replaceChildren()
        const add = (text: string, className?: string) => {
            const span = document.createElement('span')
            span.textContent = text
            if (className) span.className = className
            summary.append(span)
        }

        // The last build's status says nothing about whether the results are current, so a lost connection is shown first.
        if (!state.connected) {
            root.dataset.state = hasConnected ? 'disconnected' : 'connecting'
            add(hasConnected ? 'Disconnected, retrying…' : 'Connecting…')
        } else if (state.status === 'building' && total === 0) {
            root.dataset.state = 'building'
            add('Building...')
        } else if (total > 0) {
            root.dataset.state = 'issues'
            add(`${total} issue${total === 1 ? '' : 's'}`)
            if (state.errors)
                add(`${state.errors} errors`, 'diagnostics__count--error')
            if (state.warnings)
                add(`${state.warnings} warnings`, 'diagnostics__count--warning')
            if (state.hints)
                add(`${state.hints} hints`, 'diagnostics__count--hint')
        } else {
            root.dataset.state = 'clear'
            add('All clear')
        }
    }

    function renderList() {
        list.replaceChildren(
            ...visibleDiagnostics(state.diagnostics, filter).map((d) => {
                const row = template.content.cloneNode(true) as DocumentFragment
                const item = row.querySelector<HTMLElement>('li')!
                item.dataset.severity = d.severity
                row.querySelector('.diagnostics__severity')!.textContent =
                    d.severity
                const location = row.querySelector<HTMLElement>(
                    '.diagnostics__location'
                )!
                location.textContent = locationOf(d)
                location.title = d.file
                row.querySelector('.diagnostics__message')!.textContent =
                    d.message
                row.querySelector<HTMLElement>(
                    '[data-diagnostics-copy]'
                )!.dataset.text = clipboardTextOf(d)
                return row
            })
        )
        const empty = q('[data-diagnostics-empty]')
        empty.textContent =
            state.status === 'building'
                ? 'Waiting for diagnostics...'
                : state.diagnostics.length === 0
                  ? 'No messages to display'
                  : 'No diagnostics match the current filters'
    }

    function render() {
        renderSummary()
        renderList()
    }

    async function poll() {
        try {
            const response = await fetch(STATE_URL, { cache: 'no-store' })
            if (!response.ok) {
                state = { ...state, connected: false }
            } else {
                const event = (await response.json()) as BuildEvent
                state = { ...applyEvent(state, event), connected: true }
                hasConnected = true
            }
        } catch {
            state = { ...state, connected: false }
        }
        render()
    }

    toggle.addEventListener('click', () => setOpen(hud.hidden))
    q('[data-diagnostics-close]').addEventListener('click', () =>
        setOpen(false)
    )
    root.querySelectorAll<HTMLButtonElement>(
        '[data-diagnostics-filter]'
    ).forEach((button) =>
        button.addEventListener('click', () => {
            filter = button.dataset.diagnosticsFilter as Filter
            root.querySelectorAll<HTMLButtonElement>(
                '[data-diagnostics-filter]'
            ).forEach((b) =>
                b.setAttribute('aria-pressed', String(b === button))
            )
            renderList()
        })
    )
    list.addEventListener('click', (event) => {
        const button = (event.target as Element).closest<HTMLElement>(
            '[data-diagnostics-copy]'
        )
        if (!button?.dataset.text) return
        void navigator.clipboard?.writeText(button.dataset.text).then(() => {
            button.textContent = 'Copied'
            window.setTimeout(() => (button.textContent = 'Copy'), 2000)
        })
    })

    const setHeight = (value: number) => {
        height = Math.max(
            MIN_HEIGHT,
            Math.min(value, window.innerHeight * 0.66)
        )
        root.style.setProperty('--diagnostics-height', `${height}px`)
    }
    resize.addEventListener('pointerdown', (event) => {
        const startY = event.clientY
        const startHeight = height
        resize.setPointerCapture?.(event.pointerId)
        const move = (e: PointerEvent) =>
            setHeight(startHeight + startY - e.clientY)
        const up = () => {
            resize.removeEventListener('pointermove', move)
            resize.removeEventListener('pointerup', up)
        }
        resize.addEventListener('pointermove', move)
        resize.addEventListener('pointerup', up)
    })
    resize.addEventListener('keydown', (event) => {
        if (event.key !== 'ArrowUp' && event.key !== 'ArrowDown') return
        event.preventDefault()
        setHeight(height + (event.key === 'ArrowUp' ? 1 : -1) * KEYBOARD_STEP)
    })

    setOpen(readOpen())
    render()
    void poll()
    window.setInterval(() => void poll(), POLL_MS)
}
