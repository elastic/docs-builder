import {
    fetchEventSource,
    EventSourceMessage,
    EventStreamContentType,
} from '@microsoft/fetch-event-source'
import { useRef, useCallback } from 'react'

/**
 * Computes SHA256 hash of the request body for CloudFront + Lambda Function URL with OAC.
 * Required by AWS CloudFront when using Origin Access Control with Lambda Function URLs.
 * See: https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/private-content-restricting-access-to-lambda.html
 */
async function computeSHA256(data: string): Promise<string> {
    const encoder = new TextEncoder()
    const dataBuffer = encoder.encode(data)
    const hashBuffer = await crypto.subtle.digest('SHA-256', dataBuffer)
    const hashArray = Array.from(new Uint8Array(hashBuffer))
    return hashArray.map((b) => ('0' + b.toString(16)).slice(-2)).join('')
}

// Simple wrapper interface around fetch-event-source
export interface UseFetchEventSourceOptions {
    apiEndpoint: string
    headers?: Record<string, string>
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
    headers,
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
                // Stringify payload once to ensure hash matches the exact body sent
                const bodyString = JSON.stringify(payload)

                // Compute SHA256 hash for CloudFront + Lambda Function URL with OAC
                // This proves body integrity from client to CloudFront
                const contentHash = await computeSHA256(bodyString)

                await fetchEventSource(apiEndpoint, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'x-amz-content-sha256': contentHash, // Required for CloudFront OAC
                        ...headers,
                    },
                    body: bodyString,
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
        [apiEndpoint, headers, onMessage, onError, onOpen, onClose]
    )

    return {
        sendMessage,
        abort,
    }
}
