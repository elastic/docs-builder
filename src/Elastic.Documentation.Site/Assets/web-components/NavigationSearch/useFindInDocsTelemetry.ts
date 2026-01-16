/**
 * Centralized telemetry hook for the "Find in Docs" component.
 * Provides functions to track quality, performance, user behavior, and errors.
 *
 * Uses "find_in_docs" naming to distinguish from the advanced Search page.
 */
import { logInfo, logWarn } from '../../telemetry/logging'
import {
    ATTR_FIND_IN_DOCS_QUERY,
    ATTR_FIND_IN_DOCS_QUERY_LENGTH,
    ATTR_FIND_IN_DOCS_RESULTS_TOTAL,
    ATTR_FIND_IN_DOCS_RESULT_URL,
    ATTR_FIND_IN_DOCS_RESULT_POSITION,
    ATTR_FIND_IN_DOCS_RESULT_SCORE,
    ATTR_FIND_IN_DOCS_TRIGGER,
    ATTR_FIND_IN_DOCS_CLOSE_REASON,
    ATTR_FIND_IN_DOCS_HAD_RESULTS,
    ATTR_FIND_IN_DOCS_HAD_SELECTION,
    ATTR_FIND_IN_DOCS_NAVIGATION_METHOD,
    ATTR_FIND_IN_DOCS_NAVIGATION_DIRECTION,
    ATTR_FIND_IN_DOCS_RETRY_AFTER,
    ATTR_ERROR_TYPE,
    ATTR_EXCEPTION_MESSAGE,
} from '../../telemetry/semconv'
import { useCallback } from 'react'

export type FindInDocsTrigger = 'keyboard_shortcut' | 'focus' | 'click'
export type FindInDocsCloseReason = 'escape' | 'blur' | 'navigate' | 'clear'
export type NavigationMethod = 'keyboard' | 'mouse'
export type NavigationDirection = 'up' | 'down'

interface ResultClickParams {
    query: string
    position: number
    url: string
    score: number
}

interface ClosedParams {
    reason: FindInDocsCloseReason
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

export const useFindInDocsTelemetry = () => {
    /**
     * Track when user opens Find in Docs (focus or keyboard shortcut)
     */
    const trackOpened = useCallback((trigger: FindInDocsTrigger) => {
        logInfo('find_in_docs_opened', {
            [ATTR_FIND_IN_DOCS_TRIGGER]: trigger,
        })
    }, [])

    /**
     * Track when Find in Docs is closed
     */
    const trackClosed = useCallback(
        ({ reason, query, hadResults, hadSelection }: ClosedParams) => {
            logInfo('find_in_docs_closed', {
                [ATTR_FIND_IN_DOCS_CLOSE_REASON]: reason,
                [ATTR_FIND_IN_DOCS_QUERY]: query,
                [ATTR_FIND_IN_DOCS_HAD_RESULTS]: hadResults,
                [ATTR_FIND_IN_DOCS_HAD_SELECTION]: hadSelection,
            })
        },
        []
    )

    /**
     * Track when user clicks on a result
     */
    const trackResultClicked = useCallback(
        ({ query, position, url, score }: ResultClickParams) => {
            logInfo('find_in_docs_result_clicked', {
                [ATTR_FIND_IN_DOCS_QUERY]: query,
                [ATTR_FIND_IN_DOCS_RESULT_POSITION]: position,
                [ATTR_FIND_IN_DOCS_RESULT_URL]: url,
                [ATTR_FIND_IN_DOCS_RESULT_SCORE]: score,
            })
        },
        []
    )

    /**
     * Track when Find in Docs returns zero results
     */
    const trackZeroResults = useCallback((query: string) => {
        logInfo('find_in_docs_zero_results', {
            [ATTR_FIND_IN_DOCS_QUERY]: query,
            [ATTR_FIND_IN_DOCS_QUERY_LENGTH]: query.length,
            [ATTR_FIND_IN_DOCS_RESULTS_TOTAL]: 0,
        })
    }, [])

    /**
     * Track keyboard/mouse navigation through results
     */
    const trackNavigation = useCallback(
        ({ method, direction, query }: NavigationParams) => {
            logInfo('find_in_docs_navigation', {
                [ATTR_FIND_IN_DOCS_NAVIGATION_METHOD]: method,
                [ATTR_FIND_IN_DOCS_NAVIGATION_DIRECTION]: direction,
                [ATTR_FIND_IN_DOCS_QUERY]: query,
            })
        },
        []
    )

    /**
     * Track rate limit errors (429)
     */
    const trackRateLimited = useCallback(
        ({ query, retryAfter }: RateLimitedParams) => {
            logWarn('find_in_docs_rate_limited', {
                [ATTR_FIND_IN_DOCS_QUERY]: query,
                [ATTR_FIND_IN_DOCS_RETRY_AFTER]: retryAfter,
            })
        },
        []
    )

    /**
     * Track errors
     */
    const trackError = useCallback(
        ({ query, errorType, errorMessage }: ErrorParams) => {
            logWarn('find_in_docs_error', {
                [ATTR_FIND_IN_DOCS_QUERY]: query,
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
