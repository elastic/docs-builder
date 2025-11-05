import { AskAiCalloutState } from './AskAi/callout-state'
import { SearchCalloutState } from './Search/callout-state'
import { CalloutState, ModalMode } from './callout-state'
import { create } from 'zustand/react'

// Create callout state instances for each domain
const calloutStates = new Map<ModalMode, CalloutState>([
    ['search', new SearchCalloutState()],
    ['askAi', new AskAiCalloutState()],
])

export const getCalloutState = (domain: ModalMode): CalloutState => {
    const state = calloutStates.get(domain)
    if (!state) {
        throw new Error(`No CalloutState found for domain: ${domain}`)
    }
    return state
}

interface CooldownState {
    cooldown: number | null
    cooldownFinishedPendingAcknowledgment: boolean
}

interface ModalState {
    isOpen: boolean
    mode: ModalMode
    cooldowns: Record<ModalMode, CooldownState>
    actions: {
        setModalMode: (mode: ModalMode) => void
        openModal: () => void
        closeModal: () => void
        toggleModal: () => void
        setCooldown: (domain: ModalMode, cooldown: number | null) => void
        notifyCooldownFinished: (domain: ModalMode) => void
        acknowledgeCooldownFinished: (domain: ModalMode) => void
    }
}

const modalStore = create<ModalState>((set, get) => ({
    isOpen: false,
    mode: 'search',
    cooldowns: {
        search: {
            cooldown: null,
            cooldownFinishedPendingAcknowledgment: false,
        },
        askAi: {
            cooldown: null,
            cooldownFinishedPendingAcknowledgment: false,
        },
    },
    actions: {
        setModalMode: (mode: ModalMode) => set({ mode }),
        openModal: () => set({ isOpen: true }),
        closeModal: () => set({ isOpen: false }),
        toggleModal: () => set((state) => ({ isOpen: !state.isOpen })),
        setCooldown: (domain: ModalMode, cooldown: number | null) => {
            const calloutState = getCalloutState(domain)
            calloutState.setCooldown(
                cooldown,
                () => get(),
                (partial) => set(partial),
                () => get().actions.notifyCooldownFinished(domain)
            )
        },
        notifyCooldownFinished: (domain: ModalMode) => {
            const calloutState = getCalloutState(domain)
            calloutState.notifyCooldownFinished(
                () => get(),
                (partial) => set(partial)
            )
        },
        acknowledgeCooldownFinished: (domain: ModalMode) => {
            const calloutState = getCalloutState(domain)
            calloutState.acknowledgeCooldownFinished(
                () => get(),
                (partial) => set(partial)
            )
        },
    },
}))

export const useModalIsOpen = () => modalStore((state) => state.isOpen)
export const useModalActions = () => modalStore((state) => state.actions)
export const useModalMode = () =>
    modalStore((state: ModalState): ModalMode => state.mode)

// Search cooldown hooks
export const useSearchCooldown = () =>
    modalStore((state) => state.cooldowns.search.cooldown)
export const useSearchCooldownFinishedPendingAcknowledgment = () =>
    modalStore(
        (state) => state.cooldowns.search.cooldownFinishedPendingAcknowledgment
    )
export const useIsSearchCooldownActive = () => {
    const countdown = useSearchCooldown()
    return countdown !== null && countdown > 0
}

// Ask AI cooldown hooks
export const useAskAiCooldown = () =>
    modalStore((state) => state.cooldowns.askAi.cooldown)
export const useAskAiCooldownFinishedPendingAcknowledgment = () =>
    modalStore(
        (state) => state.cooldowns.askAi.cooldownFinishedPendingAcknowledgment
    )
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

export { modalStore }
export type { ModalMode } from './callout-state'
