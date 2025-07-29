import { useAskAiTerm } from './search.store'
import { LlmGatewayMessage, useLlmGateway } from './useLlmGateway'
import {
    EuiFlexGroup,
    EuiFlexItem,
    EuiLoadingSpinner,
    EuiMarkdownFormat,
    EuiPanel,
    EuiSpacer,
    EuiText,
    EuiButtonIcon,
    EuiToolTip,
    useEuiTheme,
    EuiCallOut,
} from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'
import { useEffect, useRef, useState, useMemo } from 'react'
import { v4 as uuidv4 } from 'uuid'

// Helper function to accumulate AI message content
const getAccumulatedContent = (messages: LlmGatewayMessage[]) => {
    return messages
        .filter((m) => m.type === 'ai_message' || m.type === 'ai_message_chunk')
        .map((m) => m.data.content)
        .join('')
}

export const AskAiAnswer = () => {
    const { euiTheme } = useEuiTheme()
    const question = useAskAiTerm()
    const threadId = useMemo(() => uuidv4(), [question])
    const [isLoading, setLoading] = useState(true)
    const [isAnswerFinished, setAnswerFinished] = useState(false)

    const scrollRef = useRef<HTMLDivElement>(null)

    const { messages, error, retry, sendQuestion } = useLlmGateway({
        threadId: threadId,
        onMessage: (message) => {
            switch (message.type) {
                case 'agent_start':
                    setLoading(true)
                    setAnswerFinished(false)
                    break
                case 'agent_end':
                    setAnswerFinished(true)
                    setLoading(false)
                    break
            }
        },
        onError: () => {
            setLoading(false)
            setAnswerFinished(true)
        },
    })

    useEffect(() => {
        if (question.trim()) {
            sendQuestion(question).catch(() => {
                // Send error with APM agent RUM?
            })
        }
    }, [question, sendQuestion])

    return (
        <EuiPanel
            paddingSize="none"
            hasShadow={false}
            hasBorder={false}
            css={css`
                .euiMarkdownFormat {
                    font-size: ${euiTheme.size.base};
                }
                .euiScreenReaderOnly {
                    display: none;
                }
            `}
        >
            <EuiPanel
                panelRef={scrollRef}
                paddingSize="m"
                hasShadow={false}
                hasBorder={false}
                css={css`
                    max-height: 50vh;
                    overflow-y: scroll;
                    background-color: ${euiTheme.colors.backgroundBaseSubdued};
                `}
            >
                <EuiMarkdownFormat>
                    {getAccumulatedContent(messages)}
                </EuiMarkdownFormat>
                <div>
                    {isAnswerFinished && (
                        <>
                            <EuiSpacer size="m" />
                            <EuiFlexGroup
                                responsive={false}
                                component="span"
                                gutterSize="s"
                            >
                                <EuiFlexItem grow={false}>
                                    <EuiToolTip content="This answer was helpful">
                                        <EuiButtonIcon
                                            aria-label="This answer was helpful"
                                            iconType="faceHappy"
                                            color="success"
                                        />
                                    </EuiToolTip>
                                </EuiFlexItem>
                                <EuiFlexItem grow={false}>
                                    <EuiToolTip content="This answer was not helpful">
                                        <EuiButtonIcon
                                            aria-label="This answer was not helpful"
                                            iconType="faceSad"
                                            color="danger"
                                        />
                                    </EuiToolTip>
                                </EuiFlexItem>
                                <EuiFlexItem grow={false}>
                                    <EuiToolTip content="Request a new answer">
                                        <EuiButtonIcon
                                            aria-label="Request a new answer"
                                            iconType="refresh"
                                            onClick={() => {
                                                setAnswerFinished(false)
                                                retry()
                                            }}
                                        />
                                    </EuiToolTip>
                                </EuiFlexItem>
                            </EuiFlexGroup>
                        </>
                    )}
                    {!error && isLoading && (
                        <>
                            {messages.filter(
                                (m) =>
                                    m.type === 'ai_message' ||
                                    m.type === 'ai_message_chunk'
                            ).length > 0 && <EuiSpacer size="s" />}
                            <EuiFlexGroup
                                alignItems="center"
                                gutterSize="xs"
                                responsive={false}
                            >
                                <EuiFlexItem grow={false}>
                                    <EuiLoadingSpinner size="s" />
                                </EuiFlexItem>
                                <EuiFlexItem grow={false}>
                                    <EuiText size="xs" color="subdued">
                                        Generating...
                                    </EuiText>
                                </EuiFlexItem>
                            </EuiFlexGroup>
                        </>
                    )}
                </div>
                {error && (
                    <>
                        <EuiSpacer size="m" />
                        <EuiCallOut
                            title="Sorry, there was an error"
                            color="danger"
                            iconType="error"
                        >
                            <p>
                                The Elastic Docs AI Assistant encountered an
                                error. Please try again.
                            </p>
                        </EuiCallOut>
                    </>
                )}
            </EuiPanel>
        </EuiPanel>
    )
}
