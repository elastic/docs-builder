import { create } from 'zustand/react'

interface ModalState {
    isOpen: boolean
    actions: {
        openModal: () => void
        closeModal: () => void
        toggleModal: () => void
    }
}

const askAiModalStore = create<ModalState>((set) => ({
    isOpen: false,
    actions: {
        openModal: () => set({ isOpen: true }),
        closeModal: () => set({ isOpen: false }),
        toggleModal: () => set((state) => ({ isOpen: !state.isOpen })),
    },
}))

export const useAskAiModalIsOpen = () =>
    askAiModalStore((state) => state.isOpen)
export const useAskAiModalActions = () =>
    askAiModalStore((state) => state.actions)

export { askAiModalStore }
