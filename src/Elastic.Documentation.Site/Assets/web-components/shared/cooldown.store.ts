import { create } from 'zustand/react'

export type CooldownDomain = 'search' | 'askAi'

interface CooldownStateData {
    cooldown: number | null
    awaitingNewInput: boolean
}

interface CooldownState {
    cooldowns: Record<CooldownDomain, CooldownStateData>
    actions: {
        setCooldown: (domain: CooldownDomain, cooldown: number | null) => void
        updateCooldown: (domain: CooldownDomain, cooldown: number | null) => void
        notifyCooldownFinished: (domain: CooldownDomain) => void
        acknowledgeCooldownFinished: (domain: CooldownDomain) => void
    }
}

const cooldownStore = create<CooldownState>((set) => ({
    cooldowns: {
        search: {
            cooldown: null,
            awaitingNewInput: false,
        },
        askAi: {
            cooldown: null,
            awaitingNewInput: false,
        },
    },
    actions: {
        setCooldown: (domain, cooldown) => {
            set((state) => ({
                cooldowns: {
                    ...state.cooldowns,
                    [domain]: {
                        cooldown,
                        awaitingNewInput: false,
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
                        awaitingNewInput: true,
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
                        awaitingNewInput: false,
                    },
                },
            }))
        },
    },
}))

export const useCooldownActions = () => cooldownStore((state) => state.actions)

export const useCooldownState = (domain: CooldownDomain) =>
    cooldownStore((state) => state.cooldowns[domain])

export { cooldownStore }
