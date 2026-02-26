import { logInfo } from '../../telemetry/logging'
import {
    ATTR_NAVIGATION_SEARCH_QUERY,
    ATTR_NAVIGATION_SEARCH_RESULT_POSITION,
    ATTR_NAVIGATION_SEARCH_RESULT_URL,
    ATTR_NAVIGATION_SEARCH_RESULT_SCORE,
    ATTR_NAVIGATION_SEARCH_TRIGGER,
    ATTR_NAVIGATION_SEARCH_CLOSE_REASON,
    ATTR_NAVIGATION_SEARCH_HAD_RESULTS,
    ATTR_NAVIGATION_SEARCH_HAD_SELECTION,
    ATTR_NAVIGATION_SEARCH_NAVIGATION_METHOD,
    ATTR_NAVIGATION_SEARCH_NAVIGATION_DIRECTION,
} from '../../telemetry/semconv'
import { useCallback } from 'react'

export type ModalSearchTrigger = 'keyboard_shortcut' | 'focus' | 'click'
export type ModalSearchCloseReason =
    | 'escape'
    | 'blur'
    | 'navigate'
    | 'close_button'
export type NavigationMethod = 'keyboard' | 'mouse'
export type NavigationDirection = 'up' | 'down'

interface ResultClickParams {
    query: string
    position: number
    url: string
    score: number
}

interface ClosedParams {
    reason: ModalSearchCloseReason
    query: string
    hadResults: boolean
    hadSelection: boolean
}

interface NavigationParams {
    method: NavigationMethod
    direction: NavigationDirection
    query: string
}

export const useModalSearchTelemetry = () => {
    const trackOpened = useCallback((trigger: ModalSearchTrigger) => {
        logInfo('modal_search_opened', {
            [ATTR_NAVIGATION_SEARCH_TRIGGER]: trigger,
        })
    }, [])

    const trackClosed = useCallback(
        ({ reason, query, hadResults, hadSelection }: ClosedParams) => {
            logInfo('modal_search_closed', {
                [ATTR_NAVIGATION_SEARCH_CLOSE_REASON]: reason,
                [ATTR_NAVIGATION_SEARCH_QUERY]: query,
                [ATTR_NAVIGATION_SEARCH_HAD_RESULTS]: hadResults,
                [ATTR_NAVIGATION_SEARCH_HAD_SELECTION]: hadSelection,
            })
        },
        []
    )

    const trackResultClicked = useCallback(
        ({ query, position, url, score }: ResultClickParams) => {
            logInfo('modal_search_result_clicked', {
                [ATTR_NAVIGATION_SEARCH_QUERY]: query,
                [ATTR_NAVIGATION_SEARCH_RESULT_POSITION]: position,
                [ATTR_NAVIGATION_SEARCH_RESULT_URL]: url,
                [ATTR_NAVIGATION_SEARCH_RESULT_SCORE]: score,
            })
        },
        []
    )

    const trackNavigation = useCallback(
        ({ method, direction, query }: NavigationParams) => {
            logInfo('modal_search_navigation', {
                [ATTR_NAVIGATION_SEARCH_NAVIGATION_METHOD]: method,
                [ATTR_NAVIGATION_SEARCH_NAVIGATION_DIRECTION]: direction,
                [ATTR_NAVIGATION_SEARCH_QUERY]: query,
            })
        },
        []
    )

    return {
        trackOpened,
        trackClosed,
        trackResultClicked,
        trackNavigation,
    }
}
