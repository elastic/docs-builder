import { create } from 'zustand/react'

export type ModalMode = 'search' | 'askAi'

let searchCooldownInterval: number | null = null
let askAiCooldownInterval: number | null = null

interface ModalState {
    isOpen: boolean
    mode: ModalMode
    // Separate cooldowns for Search and Ask AI
    searchCooldown: number | null
    askAiCooldown: number | null
    searchCooldownFinishedPendingAcknowledgment: boolean
    askAiCooldownFinishedPendingAcknowledgment: boolean
    actions: {
        setModalMode: (mode: ModalMode) => void
        openModal: () => void
        closeModal: () => void
        toggleModal: () => void
        setSearchCooldown: (cooldown: number | null) => void
        setAskAiCooldown: (cooldown: number | null) => void
        notifySearchCooldownFinished: () => void
        notifyAskAiCooldownFinished: () => void
        acknowledgeSearchCooldownFinished: () => void
        acknowledgeAskAiCooldownFinished: () => void
    }
}

const modalStore = create<ModalState>((set, get) => ({
    isOpen: false,
    mode: 'search',
    searchCooldown: null,
    askAiCooldown: null,
    searchCooldownFinishedPendingAcknowledgment: false,
    askAiCooldownFinishedPendingAcknowledgment: false,
    actions: {
        setModalMode: (mode: ModalMode) => set({ mode }),
        openModal: () => set({ isOpen: true }),
        closeModal: () => set({ isOpen: false }),
        toggleModal: () => set((state) => ({ isOpen: !state.isOpen })),
        setSearchCooldown: (cooldown: number | null) => {
            if (searchCooldownInterval) {
                clearInterval(searchCooldownInterval)
                searchCooldownInterval = null
            }

            set({
                searchCooldown: cooldown,
                searchCooldownFinishedPendingAcknowledgment: false,
            })

            if (cooldown && cooldown > 0) {
                searchCooldownInterval = window.setInterval(() => {
                    const currentCooldown = get().searchCooldown
                    if (currentCooldown !== null && currentCooldown > 1) {
                        set({ searchCooldown: currentCooldown - 1 })
                    } else {
                        if (searchCooldownInterval) {
                            clearInterval(searchCooldownInterval)
                            searchCooldownInterval = null
                        }
                        get().actions.notifySearchCooldownFinished()
                    }
                }, 1000)
            }
        },
        setAskAiCooldown: (cooldown: number | null) => {
            if (askAiCooldownInterval) {
                clearInterval(askAiCooldownInterval)
                askAiCooldownInterval = null
            }

            set({
                askAiCooldown: cooldown,
                askAiCooldownFinishedPendingAcknowledgment: false,
            })

            if (cooldown && cooldown > 0) {
                askAiCooldownInterval = window.setInterval(() => {
                    const currentCooldown = get().askAiCooldown
                    if (currentCooldown !== null && currentCooldown > 1) {
                        set({ askAiCooldown: currentCooldown - 1 })
                    } else {
                        if (askAiCooldownInterval) {
                            clearInterval(askAiCooldownInterval)
                            askAiCooldownInterval = null
                        }
                        get().actions.notifyAskAiCooldownFinished()
                    }
                }, 1000)
            }
        },
        notifySearchCooldownFinished: () =>
            set({
                searchCooldown: null,
                searchCooldownFinishedPendingAcknowledgment: true,
            }),
        notifyAskAiCooldownFinished: () =>
            set({
                askAiCooldown: null,
                askAiCooldownFinishedPendingAcknowledgment: true,
            }),
        acknowledgeSearchCooldownFinished: () =>
            set({ searchCooldownFinishedPendingAcknowledgment: false }),
        acknowledgeAskAiCooldownFinished: () =>
            set({ askAiCooldownFinishedPendingAcknowledgment: false }),
    },
}))

export const useModalIsOpen = () => modalStore((state) => state.isOpen)
export const useModalActions = () => modalStore((state) => state.actions)
export const useModalMode = () =>
    modalStore((state: ModalState): ModalMode => state.mode)

// Search cooldown hooks
export const useSearchCooldown = () =>
    modalStore((state) => state.searchCooldown)
export const useSearchCooldownFinishedPendingAcknowledgment = () =>
    modalStore((state) => state.searchCooldownFinishedPendingAcknowledgment)
export const useIsSearchCooldownActive = () => {
    const countdown = useSearchCooldown()
    return countdown !== null && countdown > 0
}

// Ask AI cooldown hooks
export const useAskAiCooldown = () => modalStore((state) => state.askAiCooldown)
export const useAskAiCooldownFinishedPendingAcknowledgment = () =>
    modalStore((state) => state.askAiCooldownFinishedPendingAcknowledgment)
export const useIsAskAiCooldownActive = () => {
    const countdown = useAskAiCooldown()
    return countdown !== null && countdown > 0
}

// Composite hooks for error callout state
export const useSearchErrorCalloutState = () => {
    const countdown = useSearchCooldown()
    const hasActiveCooldown = useIsSearchCooldownActive()
    const cooldownFinishedPendingAcknowledgment =
        useSearchCooldownFinishedPendingAcknowledgment()

    return {
        countdown,
        hasActiveCooldown,
        cooldownFinishedPendingAcknowledgment,
    }
}

export const useAskAiErrorCalloutState = () => {
    const countdown = useAskAiCooldown()
    const hasActiveCooldown = useIsAskAiCooldownActive()
    const cooldownFinishedPendingAcknowledgment =
        useAskAiCooldownFinishedPendingAcknowledgment()

    return {
        countdown,
        hasActiveCooldown,
        cooldownFinishedPendingAcknowledgment,
    }
}

// Legacy hooks for backward compatibility (can be removed if not needed)
// These check if EITHER cooldown is active - use domain-specific hooks instead
export const useCooldown = () => {
    const searchCooldown = useSearchCooldown()
    const askAiCooldown = useAskAiCooldown()
    // Return the longer cooldown if both are active, or whichever is active
    if (searchCooldown !== null && askAiCooldown !== null) {
        return Math.max(searchCooldown, askAiCooldown)
    }
    return searchCooldown ?? askAiCooldown
}
export const useIsCooldownActive = () => {
    const searchActive = useIsSearchCooldownActive()
    const askAiActive = useIsAskAiCooldownActive()
    return searchActive || askAiActive
}
export const useCooldownFinishedPendingAcknowledgment = () => {
    const searchFinished = useSearchCooldownFinishedPendingAcknowledgment()
    const askAiFinished = useAskAiCooldownFinishedPendingAcknowledgment()
    return searchFinished || askAiFinished
}

export { modalStore }
