import { useEffect, useRef, useState } from 'react'

/**
 * Configuration for status minimum display time
 * Adjust this value to change how long each status must be displayed before transitioning
 *
 * Recommended values:
 * - 500: Fast transitions (half second)
 * - 1000: Balanced
 * - 2000: Slower, more readable (default)
 * - 0: Disable (immediate updates)
 */
const STATUS_MIN_DISPLAY_TIME_MS = 2000

/**
 * Statuses that should always show immediately, bypassing the minimum display time
 */
const IMMEDIATE_STATUSES: string[] = [
    // 'Generating',
    // 'Gathering resources',
    // 'Searching Elastic\'s Docs for' // Search queries should show immediately
]

/**
 * Hook to ensure status messages are displayed for a minimum duration
 * to prevent rapid flickering between states.
 *
 * This is NOT a debounce (which waits for events to stop) or throttle (which limits rate).
 * Instead, it enforces a minimum "hold time" for each status before transitioning.
 */
export const useStatusMinDisplay = (
    newStatus: string | null
): string | null => {
    const [displayedStatus, setDisplayedStatus] = useState<string | null>(
        newStatus
    )
    const lastChangeTimeRef = useRef<number>(Date.now())
    const pendingStatusRef = useRef<string | null>(null)
    const timeoutRef = useRef<number | null>(null)

    useEffect(() => {
        // Clear any pending timeout
        if (timeoutRef.current) {
            clearTimeout(timeoutRef.current)
            timeoutRef.current = null
        }

        // If no status, clear immediately
        if (newStatus === null) {
            setDisplayedStatus(null)
            pendingStatusRef.current = null
            return
        }

        // If this is the first status or same as current, show immediately
        if (displayedStatus === null || displayedStatus === newStatus) {
            setDisplayedStatus(newStatus)
            lastChangeTimeRef.current = Date.now()
            pendingStatusRef.current = null
            return
        }

        // Check if new status should be shown immediately (bypass debounce)
        const shouldShowImmediately = IMMEDIATE_STATUSES.some((immediate) =>
            newStatus.includes(immediate)
        )

        if (shouldShowImmediately) {
            setDisplayedStatus(newStatus)
            lastChangeTimeRef.current = Date.now()
            pendingStatusRef.current = null
            return
        }

        // Calculate time elapsed since last status change
        const now = Date.now()
        const elapsed = now - lastChangeTimeRef.current

        if (elapsed >= STATUS_MIN_DISPLAY_TIME_MS) {
            // Enough time has passed, show new status immediately
            setDisplayedStatus(newStatus)
            lastChangeTimeRef.current = now
            pendingStatusRef.current = null
        } else {
            // Not enough time has passed, schedule the update
            pendingStatusRef.current = newStatus
            const delay = STATUS_MIN_DISPLAY_TIME_MS - elapsed

            timeoutRef.current = setTimeout(() => {
                if (pendingStatusRef.current !== null) {
                    setDisplayedStatus(pendingStatusRef.current)
                    lastChangeTimeRef.current = Date.now()
                    pendingStatusRef.current = null
                }
            }, delay)
        }

        return () => {
            if (timeoutRef.current) {
                clearTimeout(timeoutRef.current)
            }
        }
    }, [newStatus, displayedStatus])

    return displayedStatus
}
