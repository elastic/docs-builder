export type ModalMode = 'search' | 'askAi'

interface CooldownStateData {
    cooldowns: Record<
        ModalMode,
        {
            cooldown: number | null
            cooldownFinishedPendingAcknowledgment: boolean
        }
    >
}

type SetState = (partial: Partial<CooldownStateData>) => void

/**
 * Base class for managing error callout state for a specific domain (search or askAi).
 */
export abstract class CalloutState {
    protected cooldownInterval: number | null = null
    protected readonly domain: ModalMode

    constructor(domain: ModalMode) {
        this.domain = domain
    }

    getDomain(): ModalMode {
        return this.domain
    }

    /**
     * Set the cooldown value and start the countdown timer if needed.
     * This is called by the store actions.
     */
    setCooldown(
        cooldown: number | null,
        getState: () => CooldownStateData,
        setState: SetState,
        notifyFinished: () => void
    ): void {
        // Clear existing interval
        if (this.cooldownInterval !== null) {
            clearInterval(this.cooldownInterval)
            this.cooldownInterval = null
        }

        setState({
            cooldowns: {
                ...getState().cooldowns,
                [this.domain]: {
                    cooldown,
                    cooldownFinishedPendingAcknowledgment: false,
                },
            },
        })

        // Start countdown if cooldown is set
        if (cooldown && cooldown > 0) {
            this.cooldownInterval = window.setInterval(() => {
                const currentCooldown =
                    getState().cooldowns[this.domain].cooldown
                if (currentCooldown !== null && currentCooldown > 1) {
                    setState({
                        cooldowns: {
                            ...getState().cooldowns,
                            [this.domain]: {
                                ...getState().cooldowns[this.domain],
                                cooldown: currentCooldown - 1,
                            },
                        },
                    })
                } else {
                    if (this.cooldownInterval !== null) {
                        clearInterval(this.cooldownInterval)
                        this.cooldownInterval = null
                    }
                    notifyFinished()
                }
            }, 1000)
        }
    }

    notifyCooldownFinished(
        getState: () => CooldownStateData,
        setState: SetState
    ): void {
        setState({
            cooldowns: {
                ...getState().cooldowns,
                [this.domain]: {
                    cooldown: null,
                    cooldownFinishedPendingAcknowledgment: true,
                },
            },
        })
    }

    acknowledgeCooldownFinished(
        getState: () => CooldownStateData,
        setState: SetState
    ): void {
        setState({
            cooldowns: {
                ...getState().cooldowns,
                [this.domain]: {
                    ...getState().cooldowns[this.domain],
                    cooldownFinishedPendingAcknowledgment: false,
                },
            },
        })
    }

    cleanup(): void {
        if (this.cooldownInterval !== null) {
            clearInterval(this.cooldownInterval)
            this.cooldownInterval = null
        }
    }
}
