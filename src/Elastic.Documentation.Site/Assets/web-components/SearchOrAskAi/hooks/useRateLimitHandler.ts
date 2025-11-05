import { ApiError, isApiError } from '../errorHandling'
import {
    useCooldown,
    useModalActions,
    useCooldownJustFinished,
} from '../modal.store'
import { useEffect, useRef } from 'react'

/**
 * Hook to handle rate limit errors (429) and manage cooldown state.
 */
export function useRateLimitHandler(error: ApiError | Error | null) {
    const storeCooldown = useCooldown()
    const cooldownJustFinished = useCooldownJustFinished()
    const { setCooldown } = useModalActions()
    const previousErrorRetryAfterRef = useRef<number | null>(null)

    useEffect(() => {
        if (cooldownJustFinished) {
            return
        }
        if (
            error &&
            isApiError(error) &&
            (error as ApiError).statusCode === 429
        ) {
            const apiError = error as ApiError
            const retryAfter = apiError.retryAfter
            if (retryAfter !== undefined && retryAfter !== null) {
                const isNewError =
                    previousErrorRetryAfterRef.current !== retryAfter

                const shouldSetCooldown =
                    isNewError &&
                    ((storeCooldown === null &&
                        previousErrorRetryAfterRef.current === null) ||
                        (storeCooldown !== null && storeCooldown < retryAfter))

                if (shouldSetCooldown) {
                    setCooldown(retryAfter, apiError)
                    previousErrorRetryAfterRef.current = retryAfter
                }
            }
        } else if (!error) {
            previousErrorRetryAfterRef.current = null
        }
    }, [error, storeCooldown, setCooldown, cooldownJustFinished])
}
