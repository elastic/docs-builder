import { ApiError, getErrorMessage, isApiError, isRateLimitError } from './errorHandling'
import { useRateLimitHandler } from './hooks/useRateLimitHandler'
import { useIsCooldownActive } from './hooks/useIsCooldownActive'
import {
    useCooldown,
    useCooldownJustFinished,
    useLast429Error,
} from './modal.store'
import { EuiCallOut, EuiSpacer } from '@elastic/eui'

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

    const is429Error = displayError && isRateLimitError(displayError)

    useRateLimitHandler(is429Error ? displayError : null)

    const countdown = useCooldown()
    const cooldownJustFinished = useCooldownJustFinished()
    const hasActiveCooldown = useIsCooldownActive()

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
        newSyntheticError.retryAfter = countdown ?? undefined
        syntheticError = newSyntheticError
    }

    if (hasActiveCooldown && isApiError(syntheticError)) {
        syntheticError.retryAfter = countdown ?? undefined
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
