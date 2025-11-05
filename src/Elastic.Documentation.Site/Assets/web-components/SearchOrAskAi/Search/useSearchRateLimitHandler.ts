import { ApiError, isRateLimitError } from '../errorHandling'
import {
    useSearchCooldown,
    useModalActions,
    useSearchCooldownFinishedPendingAcknowledgment,
} from '../modal.store'
import { useEffect, useRef } from 'react'

/**
 * Hook to handle search rate limit errors (429) and manage cooldown state.
 */
export function useSearchRateLimitHandler(error: ApiError | Error | null) {
    const storeCooldown = useSearchCooldown()
    const cooldownFinishedPendingAcknowledgment = useSearchCooldownFinishedPendingAcknowledgment()
    const { setSearchCooldown } = useModalActions()
    const previousErrorRetryAfterRef = useRef<number | null>(null)

    useEffect(() => {
        if (cooldownFinishedPendingAcknowledgment) {
            return
        }
        if (error && isRateLimitError(error)) {
            const retryAfter = error.retryAfter
            if (retryAfter !== undefined && retryAfter !== null) {
                const isNewError =
                    previousErrorRetryAfterRef.current !== retryAfter

                const shouldSetCooldown =
                    isNewError &&
                    ((storeCooldown === null &&
                        previousErrorRetryAfterRef.current === null) ||
                        (storeCooldown !== null && storeCooldown < retryAfter))

                if (shouldSetCooldown) {
                    setSearchCooldown(retryAfter)
                    previousErrorRetryAfterRef.current = retryAfter
                }
            }
        } else if (!error) {
            previousErrorRetryAfterRef.current = null
        }
    }, [error, storeCooldown, setSearchCooldown, cooldownFinishedPendingAcknowledgment])
}

