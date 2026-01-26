import { create } from 'zustand'

export type BuildStatus = 'idle' | 'building' | 'complete'

// SessionStorage key for persisting HUD open state
const HUD_OPEN_KEY = 'diagnostics-hud-open'

// Helper to get persisted HUD state
function getPersistedHudOpen(): boolean {
    try {
        return sessionStorage.getItem(HUD_OPEN_KEY) === 'true'
    } catch {
        return false
    }
}

// Helper to persist HUD state
function persistHudOpen(open: boolean): void {
    try {
        sessionStorage.setItem(HUD_OPEN_KEY, String(open))
    } catch {
        // Ignore storage errors
    }
}

export type DiagnosticSeverity = 'error' | 'warning' | 'hint'

export interface DiagnosticItem {
    id: string
    severity: DiagnosticSeverity
    file: string
    message: string
    line?: number
    column?: number
    timestamp: number
}

export interface FilterState {
    errors: boolean
    warnings: boolean
    hints: boolean
}

export interface DiagnosticsState {
    status: BuildStatus
    errors: number
    warnings: number
    hints: number
    diagnostics: DiagnosticItem[]
    isHudOpen: boolean
    isConnected: boolean
    filters: FilterState

    // Actions
    setStatus: (status: BuildStatus) => void
    setCounts: (errors: number, warnings: number, hints: number) => void
    addDiagnostic: (diagnostic: DiagnosticItem) => void
    clearDiagnostics: () => void
    toggleHud: () => void
    setHudOpen: (open: boolean) => void
    setConnected: (connected: boolean) => void
    toggleFilter: (filter: keyof FilterState) => void
    showAllFilters: () => void
    reset: () => void
}

export const useDiagnosticsStore = create<DiagnosticsState>((set) => ({
    status: 'idle',
    errors: 0,
    warnings: 0,
    hints: 0,
    diagnostics: [],
    isHudOpen: getPersistedHudOpen(),
    isConnected: false,
    filters: { errors: true, warnings: true, hints: true },

    setStatus: (status) => set({ status }),

    setCounts: (errors, warnings, hints) => set({ errors, warnings, hints }),

    addDiagnostic: (diagnostic) =>
        set((state) => ({
            diagnostics: [...state.diagnostics, diagnostic],
        })),

    clearDiagnostics: () => set({ diagnostics: [], errors: 0, warnings: 0, hints: 0 }),

    toggleHud: () =>
        set((state) => {
            const newOpen = !state.isHudOpen
            persistHudOpen(newOpen)
            return { isHudOpen: newOpen }
        }),

    setHudOpen: (open) => {
        persistHudOpen(open)
        return set({ isHudOpen: open })
    },

    setConnected: (connected) => set({ isConnected: connected }),

    toggleFilter: (filter) =>
        set((state) => ({
            filters: { ...state.filters, [filter]: !state.filters[filter] },
        })),

    showAllFilters: () =>
        set({ filters: { errors: true, warnings: true, hints: true } }),

    reset: () =>
        set({
            status: 'idle',
            errors: 0,
            warnings: 0,
            hints: 0,
            diagnostics: [],
            filters: { errors: true, warnings: true, hints: true },
        }),
}))
