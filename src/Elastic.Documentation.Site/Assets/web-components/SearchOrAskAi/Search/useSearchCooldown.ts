import { useCooldownState, useCooldownActions } from '../cooldown.store'

export const useSearchCooldown = () => {
    const state = useCooldownState('search')
    return state.cooldown
}
