import {
    useCooldownState,
    useCooldownActions,
    CooldownDomain,
} from './cooldown.store'
import { ApiError, isRateLimitError, isApiError } from './errorHandling'
import { useEffect, useRef } from 'react'

/**
 * Generic hook to handle rate limit errors (429) and manage cooldown state.
 * @param domain - The domain to manage cooldown for ('search' or 'askAi')
 * @param error - The error to check for rate limiting
 */
export function useRateLimitHandler(
    domain: CooldownDomain,
    error: ApiError | Error | null
) {
    const state = useCooldownState(domain)
    const { setCooldown } = useCooldownActions()
    const previousErrorRetryAfterRef = useRef<number | null>(null)

    useEffect(() => {
        // Don't process if cooldown just finished and waiting for acknowledgment
        if (state.awaitingNewInput) {
            return
        }

        if (error && isApiError(error) && isRateLimitError(error)) {
            const retryAfter = error.retryAfter
            if (retryAfter !== undefined && retryAfter !== null) {
                const isNewError =
                    previousErrorRetryAfterRef.current !== retryAfter

                const shouldSetCooldown =
                    isNewError &&
                    ((state.cooldown === null &&
                        previousErrorRetryAfterRef.current === null) ||
                        (state.cooldown !== null &&
                            state.cooldown < retryAfter))

                if (shouldSetCooldown) {
                    setCooldown(domain, retryAfter)
                    previousErrorRetryAfterRef.current = retryAfter
                }
            }
        } else if (!error) {
            previousErrorRetryAfterRef.current = null
        }
    }, [error, domain, state.cooldown, state.awaitingNewInput, setCooldown])
}
