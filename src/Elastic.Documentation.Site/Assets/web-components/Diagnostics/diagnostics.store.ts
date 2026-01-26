import { create } from 'zustand'

export type BuildStatus = 'idle' | 'building' | 'complete'

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
    isHudOpen: false,
    isConnected: false,
    filters: { errors: true, warnings: true, hints: true },

    setStatus: (status) => set({ status }),

    setCounts: (errors, warnings, hints) => set({ errors, warnings, hints }),

    addDiagnostic: (diagnostic) =>
        set((state) => ({
            diagnostics: [...state.diagnostics, diagnostic],
        })),

    clearDiagnostics: () => set({ diagnostics: [], errors: 0, warnings: 0, hints: 0 }),

    toggleHud: () => set((state) => ({ isHudOpen: !state.isHudOpen })),

    setHudOpen: (open) => set({ isHudOpen: open }),

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
