import { useCooldownState, useCooldownActions } from '../cooldown.store'

export const useNavigationSearchCooldown = () => {
    const state = useCooldownState('search')
    return state.cooldown
}

export const useIsNavigationSearchAwaitingNewInput = () => {
    const state = useCooldownState('search')
    return state.awaitingNewInput
}

export const useIsNavigationSearchCooldownActive = () => {
    const countdown = useNavigationSearchCooldown()
    return countdown !== null && countdown > 0
}

export const useNavigationSearchErrorCalloutState = () => {
    const countdown = useNavigationSearchCooldown()
    const hasActiveCooldown = useIsNavigationSearchCooldownActive()
    const awaitingNewInput = useIsNavigationSearchAwaitingNewInput()

    return {
        countdown,
        hasActiveCooldown,
        awaitingNewInput,
    }
}

export const useNavigationSearchCooldownActions = () => {
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
