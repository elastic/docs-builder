import { EuiCallOut, EuiSpacer } from '@elastic/eui'
import { ApiError, getErrorMessage, isApiError } from './errorHandling'
import { useCountdownTimer } from './hooks/useCountdownTimer'
import { useRateLimitHandler } from './hooks/useRateLimitHandler'
import { useCooldownJustFinished } from './modal.store'

interface SearchOrAskAiErrorCalloutProps {
    error: ApiError | Error | null
    inline?: boolean
    title?: string
}

/**
 * Reusable error callout component for SearchOrAskAi.
 */
export function SearchOrAskAiErrorCallout({
    error,
    inline = false,
    title = 'Sorry, there was an error',
}: SearchOrAskAiErrorCalloutProps) {
    const is429Error =
        error && isApiError(error) && (error as ApiError).statusCode === 429

    useRateLimitHandler(is429Error ? error : null)

    const { countdown } = useCountdownTimer()
    const cooldownJustFinished = useCooldownJustFinished()

    const hasActiveCooldown = countdown !== null && countdown > 0

    if (is429Error && (!hasActiveCooldown || cooldownJustFinished)) {
        return null
    }

    if (!is429Error && error) {
        const errorMessage = getErrorMessage(error)
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

    if (!error && !hasActiveCooldown) {
        return null
    }

    let displayError: ApiError | Error | null = error

    if (!displayError && hasActiveCooldown) {
        const syntheticError = new Error(
            'Rate limit exceeded. Please wait before trying again.'
        ) as ApiError
        syntheticError.name = 'ApiError'
        syntheticError.statusCode = 429
        syntheticError.retryAfter = countdown
        displayError = syntheticError
    }

    if (hasActiveCooldown && displayError && isApiError(displayError)) {
        ;(displayError as ApiError).retryAfter = countdown
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

