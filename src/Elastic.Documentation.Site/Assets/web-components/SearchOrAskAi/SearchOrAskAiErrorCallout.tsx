import { EuiCallOut, EuiSpacer } from '@elastic/eui'
import { ApiError, getErrorMessage, isApiError } from './errorHandling'
import { useRateLimitHandler } from './hooks/useRateLimitHandler'
import {
    useCooldown,
    useCooldownJustFinished,
    useLast429Error,
} from './modal.store'

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
    const last429Error = useLast429Error()
    const displayError = error || last429Error

    const is429Error =
        displayError &&
        isApiError(displayError) &&
        (displayError as ApiError).statusCode === 429

    useRateLimitHandler(is429Error ? displayError : null)

    const countdown = useCooldown()
    const cooldownJustFinished = useCooldownJustFinished()

    const hasActiveCooldown = countdown !== null && countdown > 0

    if (is429Error && (!hasActiveCooldown || cooldownJustFinished)) {
        return null
    }

    if (!is429Error && displayError) {
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

    if (!displayError && !hasActiveCooldown) {
        return null
    }

    let syntheticError: ApiError | Error | null = displayError

    if (!syntheticError && hasActiveCooldown) {
        const newSyntheticError = new Error(
            'Rate limit exceeded. Please wait before trying again.'
        ) as ApiError
        newSyntheticError.name = 'ApiError'
        newSyntheticError.statusCode = 429
        newSyntheticError.retryAfter = countdown
        syntheticError = newSyntheticError
    }

    if (hasActiveCooldown && syntheticError && isApiError(syntheticError)) {
        ;(syntheticError as ApiError).retryAfter = countdown
    }

    const errorMessage = getErrorMessage(syntheticError)

    const calloutTitle = is429Error ? 'Rate limit exceeded' : title

    return (
        <>
            {!inline && <EuiSpacer size="m" />}
            <EuiCallOut
                title={calloutTitle}
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

