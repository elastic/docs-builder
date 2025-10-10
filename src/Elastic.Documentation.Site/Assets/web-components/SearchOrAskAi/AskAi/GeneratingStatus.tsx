/** @jsxImportSource @emotion/react */
import { LlmGatewayMessage } from './useLlmGateway'
import {
    EuiFlexGroup,
    EuiFlexItem,
    EuiLoadingSpinner,
    EuiText,
} from '@elastic/eui'
import * as React from 'react'

interface GeneratingStatusProps {
    llmMessages: LlmGatewayMessage[]
    isComplete?: boolean
}

const getToolCallSearchQuery = (
    messages: LlmGatewayMessage[]
): string | null => {
    const toolCallMessage = messages.find((m) => m.type === 'tool_call')
    if (!toolCallMessage) return null

    try {
        const toolCalls = toolCallMessage.data?.toolCalls
        if (toolCalls && toolCalls.length > 0) {
            const firstToolCall = toolCalls[0]
            return firstToolCall.args?.searchQuery || null
        }
    } catch (e) {
        console.error('Error extracting search query from tool call:', e)
    }

    return null
}

const hasContentStarted = (messages: LlmGatewayMessage[]): boolean => {
    return messages.some((m) => m.type === 'ai_message_chunk' && m.data.content)
}

const hasReachedReferences = (messages: LlmGatewayMessage[]): boolean => {
    const accumulatedContent = messages
        .filter((m) => m.type === 'ai_message_chunk')
        .map((m) => m.data.content)
        .join('')
    return accumulatedContent.includes('--- references ---')
}

export const GeneratingStatus = ({
    llmMessages,
    isComplete = false,
}: GeneratingStatusProps) => {
    const searchQuery = getToolCallSearchQuery(llmMessages)
    const contentStarted = hasContentStarted(llmMessages)
    const reachedReferences = hasReachedReferences(llmMessages)

    // If complete, don't show anything
    if (isComplete) {
        return null
    }

    // Loading states
    let statusText = 'Thinking'

    if (reachedReferences) {
        statusText = 'Finding sources'
    } else if (contentStarted) {
        statusText = 'Generating'
    } else if (searchQuery) {
        statusText = `Searching for "${searchQuery}"`
    }

    return (
        <EuiFlexGroup alignItems="center" gutterSize="s" responsive={false}>
            <EuiFlexItem grow={false}>
                <EuiLoadingSpinner size="s" />
            </EuiFlexItem>
            <EuiFlexItem grow={false}>
                <EuiText size="xs" color="subdued">
                    {statusText}...
                </EuiText>
            </EuiFlexItem>
        </EuiFlexGroup>
    )
}
