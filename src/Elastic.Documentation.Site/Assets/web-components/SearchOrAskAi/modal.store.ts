import { ApiError } from './errorHandling'
import { create } from 'zustand/react'

export type ModalMode = 'search' | 'askAi'

let cooldownInterval: number | null = null

interface ModalState {
    isOpen: boolean
    mode: ModalMode
    cooldown: number | null
    cooldownJustFinished: boolean
    last429Error: ApiError | null
    actions: {
        setModalMode: (mode: ModalMode) => void
        openModal: () => void
        closeModal: () => void
        toggleModal: () => void
        setCooldown: (cooldown: number | null, error?: ApiError | null) => void
        notifyCooldownFinished: () => void
        acknowledgeCooldownFinished: () => void
    }
}

const modalStore = create<ModalState>((set, get) => ({
    isOpen: false,
    mode: 'search',
    cooldown: null,
    cooldownJustFinished: false,
    last429Error: null,
    actions: {
        setModalMode: (mode: ModalMode) => set({ mode }),
        openModal: () => set({ isOpen: true }),
        closeModal: () => set({ isOpen: false }),
        toggleModal: () => set((state) => ({ isOpen: !state.isOpen })),
        setCooldown: (
            cooldown: number | null,
            error: ApiError | null = null
        ) => {
            if (cooldownInterval) {
                clearInterval(cooldownInterval)
                cooldownInterval = null
            }

            set({
                cooldown,
                cooldownJustFinished: false,
                last429Error: error || get().last429Error,
            })

            if (cooldown && cooldown > 0) {
                cooldownInterval = window.setInterval(() => {
                    const currentCooldown = get().cooldown
                    if (currentCooldown !== null && currentCooldown > 1) {
                        set({ cooldown: currentCooldown - 1 })
                    } else {
                        if (cooldownInterval) {
                            clearInterval(cooldownInterval)
                            cooldownInterval = null
                        }
                        get().actions.notifyCooldownFinished()
                    }
                }, 1000)
            }
        },
        notifyCooldownFinished: () =>
            set({
                cooldown: null,
                cooldownJustFinished: true,
                last429Error: null,
            }),
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
export const useLast429Error = () => modalStore((state) => state.last429Error)
export { modalStore }
