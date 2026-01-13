import { useCooldownState, useCooldownActions } from '../shared/cooldown.store'

export const useAskAiCooldown = () => {
    const state = useCooldownState('askAi')
    return state.cooldown
}

export const useIsAskAiAwaitingNewInput = () => {
    const state = useCooldownState('askAi')
    return state.awaitingNewInput
}

export const useIsAskAiCooldownActive = () => {
    const countdown = useAskAiCooldown()
    return countdown !== null && countdown > 0
}

export const useAskAiErrorCalloutState = () => {
    const countdown = useAskAiCooldown()
    const hasActiveCooldown = useIsAskAiCooldownActive()
    const awaitingNewInput = useIsAskAiAwaitingNewInput()

    return {
        countdown,
        hasActiveCooldown,
        awaitingNewInput,
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
