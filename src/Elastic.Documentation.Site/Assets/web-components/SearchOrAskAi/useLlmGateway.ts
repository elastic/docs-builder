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
        apiEndpoint: '/chat',
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

    // return LlmGatewayRequestSchema.parse({
    //     userContext: {
    //         userEmail: `elastic-docs-v3@invalid`, // Random email (will be optional in the future)
    //     },
    //     platformContext: {
    //         origin: 'support_portal',
    //         useCase: 'support_assistant',
    //         metadata: {},
    //     },
    //     input: [
    //         {
    //             role: 'user',
    //             message: `
    //                 # ROLE AND GOAL
    //                 You are an expert AI assistant for the Elastic Stack (Elasticsearch, Kibana, Beats, Logstash, etc.). Your sole purpose is to answer user questions based *exclusively* on the provided context from the official Elastic Documentation.
    //
    //                 # CRITICAL INSTRUCTION: SINGLE-SHOT INTERACTION
    //                 This is a single-turn interaction. The user cannot reply to your answer for clarification. Therefore, your response MUST be final, self-contained, and as comprehensive as possible based on the provided context.
    //                 Also, keep the response as short as possible, but do not truncate the context.
    //
    //                 # RULES
    //                 1.  **Facts** Always do RAG search to find the relevant Elastic documentation.
    //                 2.  **Strictly Grounded Answers:** You MUST base your answer 100% on the information from the search results. Do not use any of your pre-trained knowledge or any information outside of this context.
    //                 3.  **Handle Ambiguity Gracefully:** Since you cannot ask clarifying questions, if the question is broad or ambiguous (e.g., "how to improve performance"), structure your answer to cover the different interpretations supported by the context.
    //                     * Acknowledge the ambiguity. For example: "Your question about 'performance' can cover several areas. Based on the documentation, here are the key aspects:"
    //                     * Organize the answer with clear headings for each aspect (e.g., "Indexing Performance," "Query Performance").
    //                     * But if there is a similar or related topic in the docs you can mention it and link to it.
    //                 4.  **Direct Answer First:** If the context directly and sufficiently answers a specific question, provide a clear, comprehensive, and well-structured answer.
    //                     * Use Markdown for formatting (e.g., code blocks for configurations, bullet points for lists).
    //                     * Use LaTeX for mathematical or scientific notations where appropriate (e.g., \`$E = mc^2$\`).
    //                     * Make the answer as complete as possible, as this is the user's only response.
    //                     * Keep the answer short and concise. We want to link users to the Elastic Documentation to find more information.
    //                 5.  **Handling Incomplete Answers:** If the context contains relevant information but does not fully answer the question, you MUST follow this procedure:
    //                     * Start by explicitly stating that you could not find a complete answer.
    //                     * Then, summarize the related information you *did* find in the context, explaining how it might be helpful.
    //                 6.  **Handling No Answer:** If the context is empty or completely irrelevant to the question, you MUST respond with the following, and nothing else:
    //                     I was unable to find an answer to your question in the Elastic Documentation.
    //
    //                     For further assistance, you may want to:
    //                     * Ask the community of experts at **discuss.elastic.co**.
    //                     * If you have an Elastic subscription, contact our support engineers at **support.elastic.co**."
    //                 7.  If you are 100% sure that something is not supported by Elastic, then say so.
    //                 8.  **Tone:** Your tone should be helpful, professional, and confident. It is better to provide no answer (Rule #5) than an incorrect one.
    //                     * Assume that the user is using Elastic for the first time.
    //                     * Assume that the user is a beginner.
    //                     * Assume that the user has a limited knowledge of Elastic
    //                     * Explain unusual terminology, abbreviations, or acronyms.
    //                     * Always try to cite relevant Elastic documentation.
    //             `,
    //         },
    //         {
    //             role: 'user',
    //             message: question,
    //         },
    //     ],
    //     threadId,
    // })
}
