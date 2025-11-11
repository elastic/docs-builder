import { create } from 'zustand/react'

interface SearchState {
    searchTerm: string
    actions: {
        setSearchTerm: (term: string) => void
        clearSearchTerm: () => void
    }
}

export const searchStore = create<SearchState>((set) => ({
    searchTerm: '',
    actions: {
        setSearchTerm: (term: string) => set({ searchTerm: term }),
        clearSearchTerm: () => set({ searchTerm: '' }),
    },
}))

export const useSearchTerm = () => searchStore((state) => state.searchTerm)
export const useSearchActions = () => searchStore((state) => state.actions)
