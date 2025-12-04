import { create } from 'zustand/react'

export type TypeFilter = 'all' | 'doc' | 'api'

interface SearchState {
    searchTerm: string
    page: number
    typeFilter: TypeFilter
    actions: {
        setSearchTerm: (term: string) => void
        setPageNumber: (page: number) => void
        setTypeFilter: (filter: TypeFilter) => void
        clearSearchTerm: () => void
    }
}

export const searchStore = create<SearchState>((set) => ({
    searchTerm: '',
    page: 1,
    typeFilter: 'all',
    actions: {
        setSearchTerm: (term: string) => set({ searchTerm: term }),
        setPageNumber: (page: number) => set({ page }),
        setTypeFilter: (filter: TypeFilter) => set({ typeFilter: filter, page: 0 }),
        clearSearchTerm: () => set({ searchTerm: '', typeFilter: 'all' }),
    },
}))

export const useSearchTerm = () => searchStore((state) => state.searchTerm)
export const usePageNumber = () => searchStore((state) => state.page)
export const useTypeFilter = () => searchStore((state) => state.typeFilter)
export const useSearchActions = () => searchStore((state) => state.actions)
