import { create } from 'zustand/react'

interface SearchState {
    searchTerm: string
    askAiTerm: string
    actions: {
        setSearchTerm: (term: string) => void
        clearSearchTerm: () => void
        submitAskAiTerm: (term: string) => void
    }
}

export const searchStore = create<SearchState>((set) => ({
    searchTerm: '',
    askAiTerm: '',
    actions: {
        setSearchTerm: (term: string) => {
            set({ searchTerm: term })
            if (term === '') {
                set({ askAiTerm: '' })
            }
        },
        clearSearchTerm: () => set({ searchTerm: '', askAiTerm: '' }),
        submitAskAiTerm: (term: string) => {
            set({ askAiTerm: '' })
            set({ askAiTerm: term })
        },
    },
}))

export const useSearchTerm = () => searchStore((state) => state.searchTerm)
export const useAskAiTerm = () => searchStore((state) => state.askAiTerm)
export const useSearchActions = () => searchStore((state) => state.actions)
