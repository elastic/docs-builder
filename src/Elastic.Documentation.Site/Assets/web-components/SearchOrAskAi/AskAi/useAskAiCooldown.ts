import { useCooldownState, useCooldownActions } from '../cooldown.store'

export const useAskAiCooldown = () => {
    const state = useCooldownState('askAi')
    return state.cooldown
}

export const useAskAiCooldownFinishedPendingAcknowledgment = () => {
    const state = useCooldownState('askAi')
    return state.cooldownFinishedPendingAcknowledgment
}

export const useIsAskAiCooldownActive = () => {
    const countdown = useAskAiCooldown()
    return countdown !== null && countdown > 0
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

export const useAskAiCooldownActions = () => {
    const actions = useCooldownActions()
    return {
        setCooldown: (cooldown: number | null) =>
            actions.setCooldown('askAi', cooldown),
        updateCooldown: (cooldown: number | null) =>
            actions.updateCooldown('askAi', cooldown),
        notifyCooldownFinished: () => actions.notifyCooldownFinished('askAi'),
        acknowledgeCooldownFinished: () =>
            actions.acknowledgeCooldownFinished('askAi'),
    }
}

