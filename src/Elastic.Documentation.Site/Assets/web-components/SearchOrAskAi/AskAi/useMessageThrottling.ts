import { useRef, useCallback, useEffect } from 'react'

// Configuration for typing simulation
const DEFAULT_TYPING_DELAY_MS = 75

export interface UseMessageThrottlingOptions<T> {
    delayInMs?: number
    onMessage: (message: T) => void
}

export interface UseMessageThrottlingReturn<T> {
    processMessage: (message: T) => void
    clearQueue: () => void
}

export function useMessageThrottling<T>({
    delayInMs = DEFAULT_TYPING_DELAY_MS,
    onMessage,
}: UseMessageThrottlingOptions<T>): UseMessageThrottlingReturn<T> {
    const messageQueueRef = useRef<T[]>([])
    const timerRef = useRef<NodeJS.Timeout | null>(null)
    const isProcessingRef = useRef<boolean>(false)

    const processNextMessage = useCallback(() => {
        if (messageQueueRef.current.length === 0) {
            isProcessingRef.current = false
            timerRef.current = null
            return
        }

        const nextMessage = messageQueueRef.current.shift()!
        onMessage(nextMessage)

        if (messageQueueRef.current.length > 0) {
            timerRef.current = setTimeout(processNextMessage, delayInMs)
        } else {
            isProcessingRef.current = false
            timerRef.current = null
        }
    }, [onMessage, delayInMs])

    const processMessage = useCallback(
        (message: T) => {
            messageQueueRef.current.push(message)
            if (!isProcessingRef.current) {
                isProcessingRef.current = true
                timerRef.current = setTimeout(processNextMessage, delayInMs)
            }
        },
        [processNextMessage, delayInMs]
    )

    const clearQueue = useCallback(() => {
        if (timerRef.current) {
            clearTimeout(timerRef.current)
            timerRef.current = null
        }
        messageQueueRef.current = []
        isProcessingRef.current = false
    }, [])

    useEffect(() => {
        return () => {
            clearQueue()
        }
    }, [clearQueue])

    return {
        processMessage,
        clearQueue,
    }
}
