import { useEffect, useRef } from 'react'
import { useCooldown, useModalActions } from '../modal.store'
import { ApiError, isApiError } from '../errorHandling'

/**
 * Hook to handle rate limit errors (429) and manage cooldown state.
 * Detects 429 errors, extracts retryAfter, and updates the store cooldown.
 * 
 * @param error - The error to check for rate limiting
 * @param onCountdownChange - Optional callback when countdown changes
 */
export function useRateLimitHandler(
    error: ApiError | Error | null,
    onCountdownChange?: (countdown: number | null) => void
) {
    const storeCooldown = useCooldown()
    const { setCooldown } = useModalActions()
    const previousErrorRetryAfterRef = useRef<number | null>(null)
    
    useEffect(() => {
        if (error && isApiError(error) && (error as ApiError).statusCode === 429) {
            const apiError = error as ApiError
            const retryAfter = apiError.retryAfter
            if (retryAfter !== undefined && retryAfter !== null) {
                const isNewError = previousErrorRetryAfterRef.current !== retryAfter
                
                // Only set cooldown if:
                // 1. This is a new error (different retryAfter from previous)
                // 2. AND either:
                //    - storeCooldown is null AND we haven't processed any error yet (first error)
                //    - OR storeCooldown exists and is less than this error's retryAfter (update to longer cooldown)
                // Don't reset if storeCooldown is null AND we've already processed this error (cooldown expired)
                const shouldSetCooldown = isNewError && (
                    (storeCooldown === null && previousErrorRetryAfterRef.current === null) ||
                    (storeCooldown !== null && storeCooldown < retryAfter)
                )
                
                if (shouldSetCooldown) {
                    setCooldown(retryAfter)
                    onCountdownChange?.(retryAfter)
                    previousErrorRetryAfterRef.current = retryAfter
                }
            }
        } else if (!error) {
            previousErrorRetryAfterRef.current = null
        }
    }, [error, onCountdownChange, storeCooldown, setCooldown])
}

