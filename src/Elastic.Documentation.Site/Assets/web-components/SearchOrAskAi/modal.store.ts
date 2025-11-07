import { ModalMode } from './callout-state'
import { create } from 'zustand/react'

interface ModalState {
    isOpen: boolean
    mode: ModalMode
    actions: {
        setModalMode: (mode: ModalMode) => void
        openModal: () => void
        closeModal: () => void
        toggleModal: () => void
    }
}

const modalStore = create<ModalState>((set) => ({
    isOpen: false,
    mode: 'search',
    actions: {
        setModalMode: (mode: ModalMode) => set({ mode }),
        openModal: () => set({ isOpen: true }),
        closeModal: () => set({ isOpen: false }),
        toggleModal: () => set((state) => ({ isOpen: !state.isOpen })),
    },
}))

export const useModalIsOpen = () => modalStore((state) => state.isOpen)
export const useModalActions = () => modalStore((state) => state.actions)
export const useModalMode = () =>
    modalStore((state: ModalState): ModalMode => state.mode)

export { modalStore }
export type { ModalMode } from './callout-state'
