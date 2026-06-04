import { create } from 'zustand/react'

/** -1 indicates no item is selected (e.g., before user starts typing) */
export const NO_SELECTION = -1

interface ModalSearchState {
    isOpen: boolean
    searchTerm: string
    page: number
    selectedIndex: number
    actions: {
        openModal: () => void
        closeModal: () => void
        toggleModal: () => void
        setSearchTerm: (term: string) => void
        setPageNumber: (page: number) => void
        setSelectedIndex: (index: number) => void
        clearSelection: () => void
        clearSearchTerm: () => void
    }
}

export const modalSearchStore = create<ModalSearchState>((set) => ({
    isOpen: false,
    searchTerm: '',
    page: 0,
    selectedIndex: NO_SELECTION,
    actions: {
        openModal: () => set({ isOpen: true }),
        closeModal: () =>
            set({
                isOpen: false,
                searchTerm: '',
                selectedIndex: NO_SELECTION,
            }),
        toggleModal: () =>
            set((state) => ({
                isOpen: !state.isOpen,
                ...(!state.isOpen
                    ? {}
                    : {
                          searchTerm: '',
                          selectedIndex: NO_SELECTION,
                      }),
            })),
        setSearchTerm: (term: string) =>
            set({ searchTerm: term, selectedIndex: 0 }),
        setPageNumber: (page: number) => set({ page }),
        setSelectedIndex: (index: number) => set({ selectedIndex: index }),
        clearSelection: () => set({ selectedIndex: NO_SELECTION }),
        clearSearchTerm: () =>
            set({
                searchTerm: '',
                selectedIndex: NO_SELECTION,
            }),
    },
}))

export const useModalIsOpen = () => modalSearchStore((state) => state.isOpen)
export const useSearchTerm = () => modalSearchStore((state) => state.searchTerm)
export const usePageNumber = () => modalSearchStore((state) => state.page)
export const useSelectedIndex = () =>
    modalSearchStore((state) => state.selectedIndex)
export const useModalSearchActions = () =>
    modalSearchStore((state) => state.actions)
