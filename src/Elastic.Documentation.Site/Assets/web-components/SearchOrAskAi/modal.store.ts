import { create } from 'zustand/react'

export type ModalMode = 'search' | 'askAi'

interface ModalState {
    isOpen: boolean
    mode: ModalMode
    cooldown: number | null
    cooldownJustFinished: boolean
    actions: {
        setModalMode: (mode: ModalMode) => void
        openModal: () => void
        closeModal: () => void
        toggleModal: () => void
        setCooldown: (cooldown: number | null) => void
        notifyCooldownFinished: () => void
        acknowledgeCooldownFinished: () => void
    }
}

const modalStore = create<ModalState>((set) => ({
    isOpen: false,
    mode: 'search',
    cooldown: null,
    cooldownJustFinished: false,
    actions: {
        setModalMode: (mode: ModalMode) => set({ mode }),
        openModal: () => set({ isOpen: true }),
        closeModal: () => set({ isOpen: false }),
        toggleModal: () => set((state) => ({ isOpen: !state.isOpen })),
        setCooldown: (cooldown: number | null) =>
            set({ cooldown, cooldownJustFinished: false }),
        notifyCooldownFinished: () =>
            set({ cooldown: null, cooldownJustFinished: true }),
        acknowledgeCooldownFinished: () => set({ cooldownJustFinished: false }),
    },
}))

export const useModalIsOpen = () => modalStore((state) => state.isOpen)
export const useModalActions = () => modalStore((state) => state.actions)
export const useModalMode = () =>
    modalStore((state: ModalState): ModalMode => state.mode)
export const useCooldown = () => modalStore((state) => state.cooldown)
export const useCooldownJustFinished = () =>
    modalStore((state) => state.cooldownJustFinished)
export { modalStore }
