import { useFetchEventSource } from './useFetchEventSource'
import { useMessageThrottling } from './useMessageThrottling'
import { EventSourceMessage } from '@microsoft/fetch-event-source'
import { useEffect, useState, useRef, useCallback } from 'react'
import * as z from 'zod'

export const AskAiRequestSchema = z.object({
    message: z.string(),
    threadId: z.string().optional(),
})

export type AskAiRequest = z.infer<typeof AskAiRequestSchema>

const sharedAttributes = {
    timestamp: z.number(),
    id: z.string(),
}

const Message = z.discriminatedUnion('type', [
    z.object({
        ...sharedAttributes,
        type: z.literal('agent_start'),
        data: z.object({
            input: z.object({
                system: z.number().optional(),
                human: z.number().optional(),
                ai: z.number().optional(),
            }),
            thread: z.object({
                human: z.number().optional(),
                ai: z.number().optional(),
            }),
        }),
    }),
    z.object({
        ...sharedAttributes,
        type: z.literal('agent_end'),
        data: z.object({}),
    }),
    z.object({
        ...sharedAttributes,
        type: z.literal('chat_model_start'),
        data: z.object({}),
    }),
    z.object({
        ...sharedAttributes,
        type: z.literal('chat_model_end'),
        data: z.object({
            usage: z.object({
                completion_tokens: z.number(),
                completion_tokens_details: z.object({
                    accepted_prediction_tokens: z.number(),
                    audio_tokens: z.number(),
                    reasoning_tokens: z.number(),
                    rejected_prediction_tokens: z.number(),
                }),
                prompt_tokens: z.number(),
                prompt_tokens_details: z.object({
                    audio_tokens: z.number(),
                    cached_tokens: z.number(),
                }),
                total_tokens: z.number(),
            }),
            model_name: z.string(),
        }),
    }),
    z.object({
        ...sharedAttributes,
        type: z.literal('tool_call'),
        data: z.object({
            toolCalls: z.array(
                z.object({
                    id: z.string().optional(),
                    name: z.string(),
                    args: z.any(),
                })
            ),
            id: z.string().optional(),
        }),
    }),
    z.object({
        ...sharedAttributes,
        type: z.literal('tool_message'),
        data: z.object({
            toolCallId: z.string(),
            result: z.string(),
        }),
    }),
    z.object({
        ...sharedAttributes,
        type: z.literal('ai_message'),
        data: z.object({
            content: z.string(),
        }),
    }),
    z.object({
        ...sharedAttributes,
        type: z.literal('ai_message_chunk'),
        data: z.object({
            content: z.string(),
        }),
    }),
])

export type LlmGatewayMessage = z.infer<typeof Message>

export interface UseLlmGatewayResponse {
    messages: LlmGatewayMessage[]
    abort: () => void
    retry: () => void
    error: Error | null
    sendQuestion: (question: string) => Promise<void>
}

interface Props {
    onMessage?: (message: LlmGatewayMessage) => void
    onError?: (error: Error) => void
    threadId: string
}

export const useLlmGateway = (props: Props): UseLlmGatewayResponse => {
    const [messages, setMessages] = useState<LlmGatewayMessage[]>([])
    const [error, setError] = useState<Error | null>(null)
    const lastSentQuestionRef = useRef<string>('')

    const { processMessage, clearQueue } =
        useMessageThrottling<LlmGatewayMessage>({
            delayInMs: 20, // Configurable typing delay
            onMessage: (message) => {
                setMessages((prev) => [...prev, message])
                props.onMessage?.(message)
            },
        })

    const onMessage = useCallback(
        (event: EventSourceMessage) => {
            const rawEventData = JSON.parse(event.data)
            const m = Message.parse(rawEventData[1])
            processMessage(m)
        },
        [processMessage]
    )

    const { sendMessage, abort } = useFetchEventSource<AskAiRequest>({
        apiEndpoint: '/_api/v1/ask-ai/stream',
        onMessage,
        onError: (error) => {
            setError(error)
            props.onError?.(error)
        },
    })

    const sendQuestion = useCallback(
        async (question: string) => {
            if (question.trim() && question !== lastSentQuestionRef.current) {
                abort()
                lastSentQuestionRef.current = question
                setError(null)
                const payload = createLlmGatewayRequest(
                    question,
                    props.threadId
                )

                try {
                    await sendMessage(payload)
                } catch (error) {
                    if (error instanceof Error) {
                        setError(error)
                        throw error
                    }
                }
            }
        },
        [props.threadId, sendMessage, abort]
    )

    useEffect(() => {
        return () => {
            setError(null)
            setMessages([])
            clearQueue()
        }
    }, [clearQueue])

    return {
        messages,
        error,
        sendQuestion,
        abort: () => {
            abort()
            clearQueue()
        },
        retry: () => {
            abort()
            setError(null)
            setMessages([])
            clearQueue()
            if (lastSentQuestionRef.current) {
                const payload = createLlmGatewayRequest(
                    'Please answer my previous question in a different way.',
                    props.threadId
                )
                sendMessage(payload).catch((error) => {
                    setError(error)
                })
            }
        },
    }
}

function createLlmGatewayRequest(
    message: string,
    threadId?: string
): AskAiRequest {
    return AskAiRequestSchema.parse({
        message,
        threadId,
    })
}
