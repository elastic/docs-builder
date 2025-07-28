import { create } from 'zustand/react'

interface ModalState {
    isOpen: boolean
    actions: {
        openModal: () => void
        closeModal: () => void
        toggleModal: () => void
    }
}

const modalStore = create<ModalState>((set) => ({
    isOpen: false,
    actions: {
        openModal: () => set({ isOpen: true }),
        closeModal: () => set({ isOpen: false }),
        toggleModal: () => set((state) => ({ isOpen: !state.isOpen })),
    },
}))

export const useModalIsOpen = () => modalStore((state) => state.isOpen)
export const useModalActions = () => modalStore((state) => state.actions)
