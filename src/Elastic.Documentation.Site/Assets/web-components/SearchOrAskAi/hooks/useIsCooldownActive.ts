import { useCooldown } from '../modal.store'

/**
 * Hook to check if cooldown is currently active.
 */
export function useIsCooldownActive(): boolean {
    const countdown = useCooldown()
    return countdown !== null && countdown > 0
}

