import { create } from 'zustand/react'

export type TypeFilter = 'all' | 'doc' | 'api'

interface SearchState {
    searchTerm: string
    page: number
    typeFilter: TypeFilter
    isInputFocused: boolean
    actions: {
        setSearchTerm: (term: string) => void
        setPageNumber: (page: number) => void
        setTypeFilter: (filter: TypeFilter) => void
        setInputFocused: (focused: boolean) => void
        clearSearchTerm: () => void
    }
}

export const searchStore = create<SearchState>((set) => ({
    searchTerm: '',
    page: 1,
    typeFilter: 'all',
    isInputFocused: true, // starts focused since input has autoFocus
    actions: {
        setSearchTerm: (term: string) => set({ searchTerm: term }),
        setPageNumber: (page: number) => set({ page }),
        setTypeFilter: (filter: TypeFilter) =>
            set({ typeFilter: filter, page: 0 }),
        setInputFocused: (focused: boolean) => set({ isInputFocused: focused }),
        clearSearchTerm: () =>
            set({ searchTerm: '', typeFilter: 'all', isInputFocused: true }),
    },
}))

export const useSearchTerm = () => searchStore((state) => state.searchTerm)
export const usePageNumber = () => searchStore((state) => state.page)
export const useTypeFilter = () => searchStore((state) => state.typeFilter)
export const useIsInputFocused = () =>
    searchStore((state) => state.isInputFocused)
export const useSearchActions = () => searchStore((state) => state.actions)
