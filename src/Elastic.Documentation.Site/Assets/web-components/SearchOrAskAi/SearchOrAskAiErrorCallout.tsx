import { EuiCallOut, EuiSpacer } from '@elastic/eui'
import { ApiError, getErrorMessage, isApiError } from './errorHandling'
import { useCountdownTimer } from './hooks/useCountdownTimer'
import { useRateLimitHandler } from './hooks/useRateLimitHandler'

interface SearchOrAskAiErrorCalloutProps {
    /**
     * The error to display. Can be null to show only cooldown messages.
     * For non-cooldown errors, they should be cleared externally when input changes.
     */
    error: ApiError | Error | null
    /**
     * Optional callback when countdown changes
     */
    onCountdownChange?: (countdown: number | null) => void
    /**
     * If true, renders inline without extra spacing. Defaults to false.
     */
    inline?: boolean
    /**
     * Optional title for the error callout. Defaults to "Sorry, there was an error"
     */
    title?: string
}

/**
 * Reusable error callout component for SearchOrAskAi.
 * 
 * Handles two types of errors:
 * 1. Errors with cooldown (429): These disable input while active and hide when cooldown finishes
 * 2. Other errors: These display until cleared externally (typically when input changes)
 * 
 * The component automatically:
 * - Uses useRateLimitHandler to handle 429 errors and set cooldown
 * - Uses useCountdownTimer to track countdown and detect when cooldown finishes
 * - Hides 429 errors when cooldown transitions from non-null to null
 * - Displays non-cooldown errors until they are cleared externally (error becomes null)
 */
export function SearchOrAskAiErrorCallout({
    error,
    onCountdownChange,
    inline = false,
    title = 'Sorry, there was an error',
}: SearchOrAskAiErrorCalloutProps) {
    // Handle rate limit errors and set cooldown
    useRateLimitHandler(error, onCountdownChange)
    
    // Track countdown and detect when it finishes
    const { countdown, cooldownFinished } = useCountdownTimer(onCountdownChange)
    
    // Determine if we should show the error
    const hasCooldown = countdown !== null && countdown > 0
    const is429Error = error && isApiError(error) && (error as ApiError).statusCode === 429
    
    // For 429 errors: hide when cooldown finishes (transitions from non-null to null)
    // This is the key requirement - we need to know when cooldown stopped being null to becoming null
    if (is429Error && cooldownFinished) {
        return null
    }
    
    // For 429 errors: also hide if countdown is null or <= 0
    if (is429Error && (!hasCooldown || countdown === null || countdown <= 0)) {
        return null
    }
    
    // For non-cooldown errors: display until error becomes null (cleared externally)
    // Don't show if no error and no cooldown
    if (!error && !hasCooldown) {
        return null
    }
    
    // Create display error
    let displayError: ApiError | Error | null = error
    
    // If we only have cooldown but no error, create synthetic 429 error
    if (!displayError && hasCooldown) {
        const syntheticError = new Error('Rate limit exceeded. Please wait before trying again.') as ApiError
        syntheticError.name = 'ApiError'
        syntheticError.statusCode = 429
        syntheticError.retryAfter = countdown
        displayError = syntheticError
    }
    
    // Update error's retryAfter with current countdown for display
    if (hasCooldown && displayError && isApiError(displayError)) {
        (displayError as ApiError).retryAfter = countdown ?? undefined
    }
    
    const errorMessage = getErrorMessage(displayError)
    
    return (
        <>
            {!inline && <EuiSpacer size="m" />}
            <EuiCallOut
                title={title}
                color="danger"
                iconType="error"
                size="s"
            >
                {errorMessage}
            </EuiCallOut>
            {!inline && <EuiSpacer size="s" />}
        </>
    )
}

