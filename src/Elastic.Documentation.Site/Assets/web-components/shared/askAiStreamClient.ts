/**
 * Shared streaming client for AskAI Lambda communication.
 * Handles AWS CloudFront + Lambda Function URL with OAC authentication.
 */

import {
    fetchEventSource,
    EventSourceMessage,
    EventStreamContentType,
} from '@microsoft/fetch-event-source'
import { logWarn } from '../../telemetry/logging'
import { AskAiEvent, AskAiEventSchema } from '../AskAi/AskAiEvent'
import {
    ApiError,
    createApiErrorFromResponse,
    isApiError,
} from './errorHandling'

export type AiProvider = 'AgentBuilder' | 'LlmGateway'

const API_ENDPOINT = '/docs/_api/v1/ask-ai/stream'

/**
 * Compute SHA256 hash for CloudFront + Lambda Function URL with OAC
 */
async function computeSHA256(data: string): Promise<string> {
    const encoder = new TextEncoder()
    const dataBuffer = encoder.encode(data)
    const hashBuffer = await crypto.subtle.digest('SHA-256', dataBuffer)
    const hashArray = Array.from(new Uint8Array(hashBuffer))
    return hashArray.map((b) => ('0' + b.toString(16)).slice(-2)).join('')
}

export interface StreamCallbacks {
    onEvent: (event: AskAiEvent) => void
    onError: (error: ApiError | Error) => void
    onClose?: () => void
}

export interface StreamOptions {
    message: string
    conversationId?: string | null
    aiProvider?: AiProvider
    signal?: AbortSignal
    callbacks: StreamCallbacks
}

/**
 * Start a streaming connection to the AskAI Lambda.
 * Uses proper AWS headers and SSE handling.
 */
export async function startAskAiStream(options: StreamOptions): Promise<void> {
    const {
        message,
        conversationId,
        aiProvider = 'LlmGateway',
        signal,
        callbacks,
    } = options

    const payload = {
        message,
        ...(conversationId && { conversationId }),
    }

    const bodyString = JSON.stringify(payload)
    const contentHash = await computeSHA256(bodyString)

    await fetchEventSource(API_ENDPOINT, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'x-amz-content-sha256': contentHash,
            'X-AI-Provider': aiProvider,
        },
        body: bodyString,
        signal,
        openWhenHidden: true,

        onopen: async (response) => {
            if (
                response.ok &&
                response.headers
                    .get('content-type')
                    ?.includes(EventStreamContentType)
            ) {
                return
            }
            if (!response.ok) {
                const error =
                    (await createApiErrorFromResponse(response)) ??
                    new Error('Request failed')
                callbacks.onError(error)
                throw error
            }
            throw new Error('Invalid response')
        },

        onmessage: (msg: EventSourceMessage) => {
            if (msg.event === 'FatalError') {
                callbacks.onError(new Error(msg.data))
                return
            }
            if (!msg.data?.trim()) return

            try {
                const rawData = JSON.parse(msg.data)
                const result = AskAiEventSchema.safeParse(rawData)
                if (!result.success) {
                    logWarn('Failed to parse AskAi event', {
                        'error.type': 'validation',
                        'error.issues': JSON.stringify(result.error.issues),
                    })
                    return
                }
                callbacks.onEvent(result.data)
            } catch (error) {
                logWarn('Failed to parse SSE message', {
                    'error.message':
                        error instanceof Error ? error.message : String(error),
                })
            }
        },

        onerror: (err) => {
            // Don't treat abort as an error - it's normal when user cancels
            if (err instanceof Error && err.name === 'AbortError') {
                return null
            }
            if (isApiError(err)) {
                callbacks.onError(err)
                return null
            }
            const error =
                err instanceof Error
                    ? err
                    : new Error(err?.message || 'Connection error')
            callbacks.onError(error)
            return undefined
        },

        onclose: () => {
            callbacks.onClose?.()
        },
    })
}
