import {
    fetchEventSource,
    EventSourceMessage,
    EventStreamContentType,
} from '@microsoft/fetch-event-source'
import { useRef, useCallback } from 'react'

// Simple wrapper interface around fetch-event-source
export interface UseFetchEventSourceOptions {
    apiEndpoint: string
    onMessage?: (event: EventSourceMessage) => void
    onError?: (error: Error) => void
    onOpen?: (response: Response) => Promise<void>
    onClose?: () => void
}

export interface UseFetchEventSourceReturn<TPayload> {
    sendMessage: (payload: TPayload) => Promise<void>
    abort: () => void
}

class FatalError extends Error {}

export function useFetchEventSource<TPayload>({
    apiEndpoint,
    onMessage,
    onError,
    onOpen,
    onClose,
}: UseFetchEventSourceOptions): UseFetchEventSourceReturn<TPayload> {
    const abortControllerRef = useRef<AbortController | null>(null)

    const abort = useCallback(() => {
        if (abortControllerRef.current) {
            abortControllerRef.current.abort()
            abortControllerRef.current = null
        }
    }, [])

    const sendMessage = useCallback(
        async (payload: TPayload) => {
            // Always create a completely fresh AbortController for each request
            // This prevents StrictMode from using an already-aborted controller
            const controller = new AbortController()
            abortControllerRef.current = controller

            try {
                await fetchEventSource(apiEndpoint, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(payload),
                    signal: controller.signal, // Use local controller, not ref
                    onopen: async (response: Response) => {
                        if (
                            response.ok &&
                            response.headers
                                .get('content-type')
                                ?.includes(EventStreamContentType)
                        ) {
                            onOpen?.(response)
                            return
                        } else if (
                            response.status >= 400 &&
                            response.status < 500 &&
                            response.status !== 429
                        ) {
                            throw new FatalError()
                        } else {
                            throw new FatalError()
                        }
                    },
                    onmessage: (msg: EventSourceMessage) => {
                        if (msg.event === 'FatalError') {
                            throw new FatalError(msg.data)
                        }
                        if (!msg.data || !msg.data.trim()) {
                            return
                        }
                        onMessage?.(msg)
                    },
                    onerror: (err) => {
                        throw new FatalError(err?.message || 'Connection error')
                    },
                    onclose: () => {
                        onClose?.()
                    },
                })
            } catch (error) {
                if (error instanceof Error && error.name !== 'AbortError') {
                    onError?.(error)
                }
            }
        },
        [apiEndpoint, onMessage, onError, onOpen, onClose]
    )

    return {
        sendMessage,
        abort,
    }
}
