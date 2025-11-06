import { create } from 'zustand/react'
import { ModalMode } from './callout-state'

interface CooldownStateData {
    cooldown: number | null
    cooldownFinishedPendingAcknowledgment: boolean
}

interface CooldownState {
    cooldowns: Record<ModalMode, CooldownStateData>
    actions: {
        setCooldown: (domain: ModalMode, cooldown: number | null) => void
        updateCooldown: (domain: ModalMode, cooldown: number | null) => void
        notifyCooldownFinished: (domain: ModalMode) => void
        acknowledgeCooldownFinished: (domain: ModalMode) => void
    }
}

const cooldownStore = create<CooldownState>((set) => ({
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
        setCooldown: (domain, cooldown) => {
            set((state) => ({
                cooldowns: {
                    ...state.cooldowns,
                    [domain]: {
                        cooldown,
                        cooldownFinishedPendingAcknowledgment: false,
                    },
                },
            }))
        },
        updateCooldown: (domain, cooldown) => {
            set((state) => ({
                cooldowns: {
                    ...state.cooldowns,
                    [domain]: {
                        ...state.cooldowns[domain],
                        cooldown,
                    },
                },
            }))
        },
        notifyCooldownFinished: (domain) => {
            set((state) => ({
                cooldowns: {
                    ...state.cooldowns,
                    [domain]: {
                        cooldown: null,
                        cooldownFinishedPendingAcknowledgment: true,
                    },
                },
            }))
        },
        acknowledgeCooldownFinished: (domain) => {
            set((state) => ({
                cooldowns: {
                    ...state.cooldowns,
                    [domain]: {
                        ...state.cooldowns[domain],
                        cooldownFinishedPendingAcknowledgment: false,
                    },
                },
            }))
        },
    },
}))

export const useCooldownActions = () =>
    cooldownStore((state) => state.actions)

export const useCooldownState = (domain: ModalMode) =>
    cooldownStore((state) => state.cooldowns[domain])

export { cooldownStore }
export type { ModalMode } from './callout-state'
