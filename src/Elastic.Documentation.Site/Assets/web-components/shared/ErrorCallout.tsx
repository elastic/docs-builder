import { useAskAiErrorCalloutState } from '../AskAi/useAskAiCooldown'
import { useAskAiRateLimitHandler } from '../AskAi/useAskAiRateLimitHandler'
import { useNavigationSearchErrorCalloutState } from '../NavigationSearch/useNavigationSearchCooldown'
import { useNavigationSearchRateLimitHandler } from '../NavigationSearch/useNavigationSearchRateLimitHandler'
import {
    ApiError,
    getErrorMessage,
    isApiError,
    isRateLimitError,
} from './errorHandling'
import { EuiCallOut, EuiSpacer } from '@elastic/eui'

interface ErrorCalloutProps {
    error: ApiError | Error | null
    domain: 'search' | 'askAi'
    inline?: boolean
    title?: string
}

/**
 * Reusable error callout component for NavigationSearch and AskAi.
 */
export function ErrorCallout({
    error,
    domain,
    inline = false,
    title = 'Sorry, there was an error',
}: ErrorCalloutProps) {
    // Use domain-specific hooks based on the domain prop
    const searchState = useNavigationSearchErrorCalloutState()
    const askAiState = useAskAiErrorCalloutState()
    const state = domain === 'search' ? searchState : askAiState

    // Use domain-specific rate limit handler
    useNavigationSearchRateLimitHandler(domain === 'search' ? error : null)
    useAskAiRateLimitHandler(domain === 'askAi' ? error : null)

    const is429Error = error && isRateLimitError(error)

    // Hide 429 errors when cooldown finished (user can retry)
    if (is429Error && (!state.hasActiveCooldown || state.awaitingNewInput)) {
        return null
    }

    // Show non-429 errors immediately
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

    // Show cooldown message when active (even if no error prop)
    if (!error && !state.hasActiveCooldown) {
        return null
    }

    // Create synthetic 429 error for cooldown display
    let syntheticError: ApiError | Error | null = error

    if (!syntheticError && state.hasActiveCooldown) {
        const newSyntheticError = new Error(
            'Rate limit exceeded. Please wait before trying again.'
        ) as ApiError
        newSyntheticError.name = 'ApiError'
        newSyntheticError.statusCode = 429
        newSyntheticError.retryAfter = state.countdown ?? 1
        syntheticError = newSyntheticError
    }

    if (state.hasActiveCooldown && isApiError(syntheticError)) {
        if (state.countdown !== null && state.countdown > 0) {
            syntheticError.retryAfter = state.countdown
        }
    }

    const errorMessage = getErrorMessage(syntheticError)
    const calloutTitle =
        is429Error || state.hasActiveCooldown ? 'Rate limit exceeded' : title

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
