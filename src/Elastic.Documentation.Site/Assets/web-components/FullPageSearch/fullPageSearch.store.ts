import { create } from 'zustand/react'

export type SortBy = 'relevance' | 'recent' | 'alpha'

export interface FullPageSearchFilters {
    type: string[]
    navigationSection: string[]
    deploymentType: string[]
}

interface FullPageSearchState {
    query: string
    hasSearched: boolean
    page: number
    pageSize: number
    sortBy: SortBy
    version: string
    filters: FullPageSearchFilters
    recentSearches: string[]
    actions: {
        setQuery: (query: string) => void
        submitSearch: (query: string) => void
        setPage: (page: number) => void
        setPageSize: (size: number) => void
        setSortBy: (sortBy: SortBy) => void
        setVersion: (version: string) => void
        toggleFilter: (key: keyof FullPageSearchFilters, value: string) => void
        removeFilter: (key: keyof FullPageSearchFilters, value: string) => void
        clearAllFilters: () => void
        addRecentSearch: (query: string) => void
        clearRecentSearches: () => void
        reset: () => void
        goToLanding: () => void
    }
}

const DEFAULT_VERSION = '9.0+'

const initialFilters: FullPageSearchFilters = {
    type: [],
    navigationSection: [],
    deploymentType: [],
}

// URL parameter helpers
const VERSION_TO_URL: Record<string, string> = {
    '8.19': '8',
    '7.17': '7',
}

const URL_TO_VERSION: Record<string, string> = {
    '8': '8.19',
    '7': '7.17',
}

function getStateFromUrl(): Partial<FullPageSearchState> {
    if (typeof window === 'undefined') return {}

    const params = new URLSearchParams(window.location.search)
    const state: Partial<FullPageSearchState> = {}

    const q = params.get('q')
    if (q) {
        state.query = q
        state.hasSearched = true
    }

    const v = params.get('v')
    if (v && URL_TO_VERSION[v]) {
        state.version = URL_TO_VERSION[v]
    }

    const type = params.getAll('type')
    const section = params.getAll('section')

    if (type.length > 0 || section.length > 0) {
        state.filters = {
            type: type,
            navigationSection: section,
            deploymentType: [],
        }
    }

    return state
}

function syncUrlToState(state: FullPageSearchState) {
    if (typeof window === 'undefined') return

    const params = new URLSearchParams()

    if (state.hasSearched && state.query) {
        params.set('q', state.query)
    }

    if (state.version !== DEFAULT_VERSION && VERSION_TO_URL[state.version]) {
        params.set('v', VERSION_TO_URL[state.version])
    }

    state.filters.type.forEach(t => params.append('type', t))
    state.filters.navigationSection.forEach(s => params.append('section', s))

    const newUrl = params.toString()
        ? `${window.location.pathname}?${params.toString()}`
        : window.location.pathname

    window.history.replaceState({}, '', newUrl)
}

// Get initial state from URL
const urlState = getStateFromUrl()

export const fullPageSearchStore = create<FullPageSearchState>((set, get) => ({
    query: urlState.query ?? '',
    hasSearched: urlState.hasSearched ?? false,
    page: 1,
    pageSize: 20,
    sortBy: 'relevance',
    version: urlState.version ?? DEFAULT_VERSION,
    filters: urlState.filters ?? { ...initialFilters },
    recentSearches: [],
    actions: {
        setQuery: (query: string) => set({ query }),
        submitSearch: (query: string) => {
            if (!query.trim()) return
            const { actions } = get()
            actions.addRecentSearch(query.trim())
            set({
                query: query.trim(),
                hasSearched: true,
                page: 1,
            })
        },
        setPage: (page: number) => set({ page }),
        setPageSize: (size: number) => set({ pageSize: size, page: 1 }),
        setSortBy: (sortBy: SortBy) => set({ sortBy, page: 1 }),
        setVersion: (version: string) => set({ version, page: 1 }),
        toggleFilter: (key: keyof FullPageSearchFilters, value: string) =>
            set((state) => {
                const current = state.filters[key]
                const updated = current.includes(value)
                    ? current.filter((v) => v !== value)
                    : [...current, value]
                return {
                    filters: { ...state.filters, [key]: updated },
                    page: 1,
                }
            }),
        removeFilter: (key: keyof FullPageSearchFilters, value: string) =>
            set((state) => ({
                filters: {
                    ...state.filters,
                    [key]: state.filters[key].filter((v) => v !== value),
                },
                page: 1,
            })),
        clearAllFilters: () =>
            set({
                filters: { ...initialFilters },
                version: DEFAULT_VERSION,
                page: 1,
            }),
        addRecentSearch: (query: string) =>
            set((state) => ({
                recentSearches: [
                    query,
                    ...state.recentSearches.filter((s) => s !== query),
                ].slice(0, 10),
            })),
        clearRecentSearches: () => set({ recentSearches: [] }),
        reset: () =>
            set({
                query: '',
                hasSearched: false,
                page: 1,
                pageSize: 20,
                sortBy: 'relevance',
                version: DEFAULT_VERSION,
                filters: { ...initialFilters },
            }),
        goToLanding: () => {
            set({
                query: '',
                hasSearched: false,
                page: 1,
                pageSize: 20,
                sortBy: 'relevance',
                version: DEFAULT_VERSION,
                filters: { ...initialFilters },
            })
            // Clear URL params
            if (typeof window !== 'undefined') {
                window.history.replaceState({}, '', window.location.pathname)
            }
        },
    },
}))

// Subscribe to state changes to sync URL
fullPageSearchStore.subscribe((state) => {
    syncUrlToState(state)
})

// Selectors
export const useFullPageSearchQuery = () =>
    fullPageSearchStore((state) => state.query)
export const useHasSearched = () =>
    fullPageSearchStore((state) => state.hasSearched)
export const usePage = () => fullPageSearchStore((state) => state.page)
export const usePageSize = () => fullPageSearchStore((state) => state.pageSize)
export const useSortBy = () => fullPageSearchStore((state) => state.sortBy)
export const useVersion = () => fullPageSearchStore((state) => state.version)
export const useFilters = () => fullPageSearchStore((state) => state.filters)
export const useRecentSearches = () =>
    fullPageSearchStore((state) => state.recentSearches)
export const useFullPageSearchActions = () =>
    fullPageSearchStore((state) => state.actions)

// Helper to check if any filters are active
export const useHasActiveFilters = () =>
    fullPageSearchStore((state) => {
        const { filters, version } = state
        return (
            version !== DEFAULT_VERSION ||
            filters.type.length > 0 ||
            filters.navigationSection.length > 0 ||
            filters.deploymentType.length > 0
        )
    })
