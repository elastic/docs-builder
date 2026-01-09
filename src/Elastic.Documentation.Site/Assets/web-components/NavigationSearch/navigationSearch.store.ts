import { create } from 'zustand/react'

export type TypeFilter = 'all' | 'doc' | 'api'

/** -1 indicates no item is selected (e.g., before user starts typing) */
export const NO_SELECTION = -1

interface NavigationSearchState {
    searchTerm: string
    page: number
    typeFilter: TypeFilter
    selectedIndex: number
    actions: {
        setSearchTerm: (term: string) => void
        setPageNumber: (page: number) => void
        setTypeFilter: (filter: TypeFilter) => void
        setSelectedIndex: (index: number) => void
        clearSelection: () => void
        clearSearchTerm: () => void
    }
}

export const navigationSearchStore = create<NavigationSearchState>((set) => ({
    searchTerm: '',
    page: 0,
    typeFilter: 'all',
    selectedIndex: NO_SELECTION,
    actions: {
        setSearchTerm: (term: string) =>
            set({ searchTerm: term, selectedIndex: 0 }),
        setPageNumber: (page: number) => set({ page }),
        setTypeFilter: (filter: TypeFilter) =>
            set({ typeFilter: filter, page: 0, selectedIndex: 0 }),
        setSelectedIndex: (index: number) => set({ selectedIndex: index }),
        clearSelection: () => set({ selectedIndex: NO_SELECTION }),
        clearSearchTerm: () =>
            set({
                searchTerm: '',
                typeFilter: 'all',
                selectedIndex: NO_SELECTION,
            }),
    },
}))

export const useSearchTerm = () =>
    navigationSearchStore((state) => state.searchTerm)
export const usePageNumber = () => navigationSearchStore((state) => state.page)
export const useTypeFilter = () =>
    navigationSearchStore((state) => state.typeFilter)
export const useSelectedIndex = () =>
    navigationSearchStore((state) => state.selectedIndex)
export const useSearchActions = () =>
    navigationSearchStore((state) => state.actions)
