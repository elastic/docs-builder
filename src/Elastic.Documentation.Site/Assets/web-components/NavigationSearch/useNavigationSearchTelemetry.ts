/**
 * Centralized telemetry hook for the Navigation Search component.
 * Provides functions to track quality, performance, user behavior, and errors.
 *
 * Uses "navigation_search" naming to distinguish from the advanced Search page.
 */
import { logInfo, logWarn } from '../../telemetry/logging'
import {
    ATTR_NAVIGATION_SEARCH_QUERY,
    ATTR_NAVIGATION_SEARCH_QUERY_LENGTH,
    ATTR_NAVIGATION_SEARCH_RESULTS_TOTAL,
    ATTR_NAVIGATION_SEARCH_RESULT_URL,
    ATTR_NAVIGATION_SEARCH_RESULT_POSITION,
    ATTR_NAVIGATION_SEARCH_RESULT_SCORE,
    ATTR_NAVIGATION_SEARCH_TRIGGER,
    ATTR_NAVIGATION_SEARCH_CLOSE_REASON,
    ATTR_NAVIGATION_SEARCH_HAD_RESULTS,
    ATTR_NAVIGATION_SEARCH_HAD_SELECTION,
    ATTR_NAVIGATION_SEARCH_NAVIGATION_METHOD,
    ATTR_NAVIGATION_SEARCH_NAVIGATION_DIRECTION,
    ATTR_NAVIGATION_SEARCH_RETRY_AFTER,
    ATTR_ERROR_TYPE,
    ATTR_EXCEPTION_MESSAGE,
} from '../../telemetry/semconv'
import { useCallback } from 'react'

export type NavigationSearchTrigger = 'keyboard_shortcut' | 'focus' | 'click'
export type NavigationSearchCloseReason =
    | 'escape'
    | 'blur'
    | 'navigate'
    | 'clear'
export type NavigationMethod = 'keyboard' | 'mouse'
export type NavigationDirection = 'up' | 'down'

interface ResultClickParams {
    query: string
    position: number
    url: string
    score: number
}

interface ClosedParams {
    reason: NavigationSearchCloseReason
    query: string
    hadResults: boolean
    hadSelection: boolean
}

interface NavigationParams {
    method: NavigationMethod
    direction: NavigationDirection
    query: string
}

interface RateLimitedParams {
    query: string
    retryAfter: number
}

interface ErrorParams {
    query: string
    errorType: string
    errorMessage: string
}

export const useNavigationSearchTelemetry = () => {
    /**
     * Track when user opens Navigation Search (focus or keyboard shortcut)
     */
    const trackOpened = useCallback((trigger: NavigationSearchTrigger) => {
        logInfo('navigation_search_opened', {
            [ATTR_NAVIGATION_SEARCH_TRIGGER]: trigger,
        })
    }, [])

    /**
     * Track when Navigation Search is closed
     */
    const trackClosed = useCallback(
        ({ reason, query, hadResults, hadSelection }: ClosedParams) => {
            logInfo('navigation_search_closed', {
                [ATTR_NAVIGATION_SEARCH_CLOSE_REASON]: reason,
                [ATTR_NAVIGATION_SEARCH_QUERY]: query,
                [ATTR_NAVIGATION_SEARCH_HAD_RESULTS]: hadResults,
                [ATTR_NAVIGATION_SEARCH_HAD_SELECTION]: hadSelection,
            })
        },
        []
    )

    /**
     * Track when user clicks on a result
     */
    const trackResultClicked = useCallback(
        ({ query, position, url, score }: ResultClickParams) => {
            logInfo('navigation_search_result_clicked', {
                [ATTR_NAVIGATION_SEARCH_QUERY]: query,
                [ATTR_NAVIGATION_SEARCH_RESULT_POSITION]: position,
                [ATTR_NAVIGATION_SEARCH_RESULT_URL]: url,
                [ATTR_NAVIGATION_SEARCH_RESULT_SCORE]: score,
            })
        },
        []
    )

    /**
     * Track when Navigation Search returns zero results
     */
    const trackZeroResults = useCallback((query: string) => {
        logInfo('navigation_search_zero_results', {
            [ATTR_NAVIGATION_SEARCH_QUERY]: query,
            [ATTR_NAVIGATION_SEARCH_QUERY_LENGTH]: query.length,
            [ATTR_NAVIGATION_SEARCH_RESULTS_TOTAL]: 0,
        })
    }, [])

    /**
     * Track keyboard/mouse navigation through results
     */
    const trackNavigation = useCallback(
        ({ method, direction, query }: NavigationParams) => {
            logInfo('navigation_search_navigation', {
                [ATTR_NAVIGATION_SEARCH_NAVIGATION_METHOD]: method,
                [ATTR_NAVIGATION_SEARCH_NAVIGATION_DIRECTION]: direction,
                [ATTR_NAVIGATION_SEARCH_QUERY]: query,
            })
        },
        []
    )

    /**
     * Track rate limit errors (429)
     */
    const trackRateLimited = useCallback(
        ({ query, retryAfter }: RateLimitedParams) => {
            logWarn('navigation_search_rate_limited', {
                [ATTR_NAVIGATION_SEARCH_QUERY]: query,
                [ATTR_NAVIGATION_SEARCH_RETRY_AFTER]: retryAfter,
            })
        },
        []
    )

    /**
     * Track errors
     */
    const trackError = useCallback(
        ({ query, errorType, errorMessage }: ErrorParams) => {
            logWarn('navigation_search_error', {
                [ATTR_NAVIGATION_SEARCH_QUERY]: query,
                [ATTR_ERROR_TYPE]: errorType,
                [ATTR_EXCEPTION_MESSAGE]: errorMessage,
            })
        },
        []
    )

    return {
        trackOpened,
        trackClosed,
        trackResultClicked,
        trackZeroResults,
        trackNavigation,
        trackRateLimited,
        trackError,
    }
}
