import { create } from 'zustand/react'

interface SearchState {
    searchTerm: string
    page: number
    actions: {
        setSearchTerm: (term: string) => void
        setPageNumber: (page: number) => void
        clearSearchTerm: () => void
    }
}

export const searchStore = create<SearchState>((set) => ({
    searchTerm: '',
    page: 1,
    actions: {
        setSearchTerm: (term: string) => set({ searchTerm: term }),
        setPageNumber: (page: number) => set({ page }),
        clearSearchTerm: () => set({ searchTerm: '' }),
    },
}))

export const useSearchTerm = () => searchStore((state) => state.searchTerm)
export const usePageNumber = () => searchStore((state) => state.page)
export const useSearchActions = () => searchStore((state) => state.actions)
