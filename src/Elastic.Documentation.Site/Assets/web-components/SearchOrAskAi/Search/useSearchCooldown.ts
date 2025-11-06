import { useCooldownState, useCooldownActions } from '../cooldown.store'

export const useSearchCooldown = () => {
    const state = useCooldownState('search')
    return state.cooldown
}

export const useSearchCooldownFinishedPendingAcknowledgment = () => {
    const state = useCooldownState('search')
    return state.cooldownFinishedPendingAcknowledgment
}

export const useIsSearchCooldownActive = () => {
    const countdown = useSearchCooldown()
    return countdown !== null && countdown > 0
}

export const useSearchCooldownActions = () => {
    const actions = useCooldownActions()
    return {
        setCooldown: (cooldown: number | null) =>
            actions.setCooldown('search', cooldown),
        updateCooldown: (cooldown: number | null) =>
            actions.updateCooldown('search', cooldown),
        notifyCooldownFinished: () => actions.notifyCooldownFinished('search'),
        acknowledgeCooldownFinished: () =>
            actions.acknowledgeCooldownFinished('search'),
    }
}
