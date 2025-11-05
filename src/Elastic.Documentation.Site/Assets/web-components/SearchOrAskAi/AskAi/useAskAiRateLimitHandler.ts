import { ApiError, isRateLimitError } from '../errorHandling'
import {
    useAskAiCooldown,
    useModalActions,
    useAskAiCooldownFinishedPendingAcknowledgment,
} from '../modal.store'
import { useEffect, useRef } from 'react'

/**
 * Hook to handle Ask AI rate limit errors (429) and manage cooldown state.
 */
export function useAskAiRateLimitHandler(error: ApiError | Error | null) {
    const storeCooldown = useAskAiCooldown()
    const cooldownFinishedPendingAcknowledgment = useAskAiCooldownFinishedPendingAcknowledgment()
    const { setAskAiCooldown } = useModalActions()
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
                    setAskAiCooldown(retryAfter)
                    previousErrorRetryAfterRef.current = retryAfter
                }
            }
        } else if (!error) {
            previousErrorRetryAfterRef.current = null
        }
    }, [error, storeCooldown, setAskAiCooldown, cooldownFinishedPendingAcknowledgment])
}

