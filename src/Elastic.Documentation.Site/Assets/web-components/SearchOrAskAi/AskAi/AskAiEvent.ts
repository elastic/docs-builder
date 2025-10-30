// Canonical AskAI event types - matches backend AskAiEvent records
import * as z from 'zod'

// Event type constants for type-safe referencing
export const EventTypes = {
    CONVERSATION_START: 'conversation_start',
    CHUNK: 'chunk',
    CHUNK_COMPLETE: 'chunk_complete',
    SEARCH_TOOL_CALL: 'search_tool_call',
    TOOL_CALL: 'tool_call',
    TOOL_RESULT: 'tool_result',
    REASONING: 'reasoning',
    CONVERSATION_END: 'conversation_end',
    ERROR: 'error',
} as const

// Individual event schemas
export const ConversationStartEventSchema = z.object({
    type: z.literal(EventTypes.CONVERSATION_START),
    id: z.string(),
    timestamp: z.number(),
    conversationId: z.string(),
})

export const ChunkEventSchema = z.object({
    type: z.literal(EventTypes.CHUNK),
    id: z.string(),
    timestamp: z.number(),
    content: z.string(),
})

export const ChunkCompleteEventSchema = z.object({
    type: z.literal(EventTypes.CHUNK_COMPLETE),
    id: z.string(),
    timestamp: z.number(),
    fullContent: z.string(),
})

export const SearchToolCallEventSchema = z.object({
    type: z.literal(EventTypes.SEARCH_TOOL_CALL),
    id: z.string(),
    timestamp: z.number(),
    toolCallId: z.string(),
    searchQuery: z.string(),
})

export const ToolCallEventSchema = z.object({
    type: z.literal(EventTypes.TOOL_CALL),
    id: z.string(),
    timestamp: z.number(),
    toolCallId: z.string(),
    toolName: z.string(),
    arguments: z.string(),
})

export const ToolResultEventSchema = z.object({
    type: z.literal(EventTypes.TOOL_RESULT),
    id: z.string(),
    timestamp: z.number(),
    toolCallId: z.string(),
    result: z.string(),
})

export const ReasoningEventSchema = z.object({
    type: z.literal(EventTypes.REASONING),
    id: z.string(),
    timestamp: z.number(),
    message: z.string().nullable(),
})

export const ConversationEndEventSchema = z.object({
    type: z.literal(EventTypes.CONVERSATION_END),
    id: z.string(),
    timestamp: z.number(),
})

export const ErrorEventSchema = z.object({
    type: z.literal(EventTypes.ERROR),
    id: z.string(),
    timestamp: z.number(),
    message: z.string(),
})

// Discriminated union of all event types
export const AskAiEventSchema = z.discriminatedUnion('type', [
    ConversationStartEventSchema,
    ChunkEventSchema,
    ChunkCompleteEventSchema,
    SearchToolCallEventSchema,
    ToolCallEventSchema,
    ToolResultEventSchema,
    ReasoningEventSchema,
    ConversationEndEventSchema,
    ErrorEventSchema,
])

// Infer TypeScript types from schemas
export type ConversationStartEvent = z.infer<
    typeof ConversationStartEventSchema
>
export type ChunkEvent = z.infer<typeof ChunkEventSchema>
export type ChunkCompleteEvent = z.infer<typeof ChunkCompleteEventSchema>
export type SearchToolCallEvent = z.infer<typeof SearchToolCallEventSchema>
export type ToolCallEvent = z.infer<typeof ToolCallEventSchema>
export type ToolResultEvent = z.infer<typeof ToolResultEventSchema>
export type ReasoningEvent = z.infer<typeof ReasoningEventSchema>
export type ConversationEndEvent = z.infer<typeof ConversationEndEventSchema>
export type ErrorEvent = z.infer<typeof ErrorEventSchema>
export type AskAiEvent = z.infer<typeof AskAiEventSchema>
