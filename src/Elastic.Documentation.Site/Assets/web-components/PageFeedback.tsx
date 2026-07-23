import { config } from '../config'
import r2wc from '@r2wc/react-to-web-component'
import * as React from 'react'
import { FormEvent, useEffect, useId, useRef, useState } from 'react'

const COMMENT_MAX_LENGTH = 2000
const COLLAPSE_ANIMATION_DURATION = 200
const REACTION_DEBOUNCE_DURATION = 500

type Reaction = 'thumbsUp' | 'thumbsDown'

interface PageFeedbackProps {
    pageUrl: string
    pageTitle: string
}

interface PageFeedbackPayload {
    pageUrl: string
    pageTitle: string
    reaction: Reaction
    comment?: string
}

const ThumbIcon = ({ down = false }: { down?: boolean }) => (
    <svg
        aria-hidden="true"
        fill="none"
        viewBox="0 0 24 24"
        stroke="currentColor"
        strokeWidth="1.5"
        style={down ? { transform: 'rotate(180deg)' } : undefined}
    >
        <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M7.5 10.5 10 3.75a1.875 1.875 0 0 1 3.61.99l-.58 3.26h4.72a2.25 2.25 0 0 1 2.2 2.72l-1.45 6.75A2.25 2.25 0 0 1 16.3 19.25H7.5m0-8.75v8.75m0-8.75H4.875A1.875 1.875 0 0 0 3 12.375v5A1.875 1.875 0 0 0 4.875 19.25H7.5"
        />
    </svg>
)

const submitFeedback = async (
    feedbackId: string,
    payload: PageFeedbackPayload
) => {
    const response = await fetch(
        `${config.apiBasePath}/v1/page-feedback/${feedbackId}`,
        {
            method: 'PUT',
            credentials: 'same-origin',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(payload),
        }
    )

    if (!response.ok) {
        throw new Error(
            `Feedback request failed with status ${response.status}`
        )
    }
}

const revokeFeedback = async (feedbackId: string) => {
    const response = await fetch(
        `${config.apiBasePath}/v1/page-feedback/${feedbackId}`,
        {
            method: 'DELETE',
            credentials: 'same-origin',
        }
    )

    if (!response.ok) {
        throw new Error(
            `Feedback deletion failed with status ${response.status}`
        )
    }
}

export const PageFeedback = ({ pageUrl, pageTitle }: PageFeedbackProps) => {
    const feedbackRef = useRef<HTMLElement>(null)
    const commentRef = useRef<HTMLTextAreaElement>(null)
    const reactionTimeoutRef = useRef<number | undefined>(undefined)
    const feedbackMayExistRef = useRef(false)
    const questionId = useId()
    const guidanceId = useId()
    const [feedbackId] = useState(() => crypto.randomUUID())
    const [reaction, setReaction] = useState<Reaction | null>(null)
    const [comment, setComment] = useState('')
    const [savedComment, setSavedComment] = useState<string | undefined>()
    const [showComment, setShowComment] = useState(false)
    const [isClosing, setIsClosing] = useState(false)
    const [isSaving, setIsSaving] = useState(false)
    const [showThanks, setShowThanks] = useState(false)
    const [error, setError] = useState(false)

    useEffect(() => {
        if (!showComment || isClosing) return

        const closeOnOutsideClick = (event: PointerEvent) => {
            if (
                event.target instanceof Node &&
                !feedbackRef.current?.contains(event.target)
            ) {
                setIsClosing(true)
            }
        }

        document.addEventListener('pointerdown', closeOnOutsideClick)
        return () =>
            document.removeEventListener('pointerdown', closeOnOutsideClick)
    }, [isClosing, showComment])

    useEffect(() => {
        if (!isClosing) return

        const timeout = window.setTimeout(() => {
            setShowComment(false)
            setIsClosing(false)
        }, COLLAPSE_ANIMATION_DURATION)

        return () => window.clearTimeout(timeout)
    }, [isClosing])

    useEffect(() => {
        if (showComment && !isClosing) commentRef.current?.focus()
    }, [isClosing, showComment])

    useEffect(() => () => window.clearTimeout(reactionTimeoutRef.current), [])

    const queueReaction = (nextReaction: Reaction | null) => {
        window.clearTimeout(reactionTimeoutRef.current)

        if (!nextReaction && !feedbackMayExistRef.current) return

        reactionTimeoutRef.current = window.setTimeout(() => {
            if (!nextReaction) {
                void revokeFeedback(feedbackId)
                    .then(() => {
                        feedbackMayExistRef.current = false
                    })
                    .catch(() => undefined)
                return
            }

            feedbackMayExistRef.current = true
            void submitFeedback(feedbackId, {
                pageUrl,
                pageTitle,
                reaction: nextReaction,
                comment: savedComment,
            }).catch(() => undefined)
        }, REACTION_DEBOUNCE_DURATION)
    }

    const submitCommentFeedback = async (payload: PageFeedbackPayload) => {
        window.clearTimeout(reactionTimeoutRef.current)
        feedbackMayExistRef.current = true
        setIsSaving(true)
        setError(false)

        try {
            await submitFeedback(feedbackId, payload)
            setSavedComment(payload.comment)
            setComment(payload.comment ?? '')
            setIsClosing(true)
            setShowThanks(true)
        } catch {
            setError(true)
        } finally {
            setIsSaving(false)
        }
    }

    const selectReaction = (nextReaction: Reaction) => {
        if (isSaving) return

        const selectedReaction = reaction === nextReaction ? null : nextReaction
        setReaction(selectedReaction)
        setShowThanks(false)
        setError(false)
        queueReaction(selectedReaction)

        if (!selectedReaction) {
            if (showComment) setIsClosing(true)
            return
        }

        setIsClosing(false)
        setShowComment(true)
    }

    const submitComment = (event: FormEvent) => {
        event.preventDefault()
        const trimmedComment = comment.trim()
        if (!reaction || !trimmedComment || isSaving) return

        void submitCommentFeedback({
            pageUrl,
            pageTitle,
            reaction,
            comment: trimmedComment,
        })
    }

    const reopenComment = (event: React.MouseEvent<HTMLElement>) => {
        if (
            !reaction ||
            (showComment && !isClosing) ||
            (event.target instanceof Element &&
                event.target.closest('button, input, textarea, form, a'))
        ) {
            return
        }

        setIsClosing(false)
        setShowComment(true)
        setShowThanks(false)
    }

    return (
        <section
            ref={feedbackRef}
            className={`page-feedback${reaction ? ` page-feedback--${reaction === 'thumbsUp' ? 'positive' : 'negative'}` : ''}${showComment ? ' page-feedback--expanded' : ''}${isClosing ? ' page-feedback--closing' : ''}${reaction && !showComment ? ' page-feedback--collapsed' : ''}`}
            aria-labelledby={questionId}
            onClick={reopenComment}
            onBlur={(event) => {
                if (
                    event.relatedTarget &&
                    !event.currentTarget.contains(event.relatedTarget)
                ) {
                    setIsClosing(true)
                }
            }}
        >
            <div className="page-feedback__prompt">
                <p id={questionId} className="page-feedback__question">
                    Was this page helpful?
                </p>
                <div
                    className="page-feedback__reactions"
                    role="group"
                    aria-labelledby={questionId}
                >
                    <button
                        type="button"
                        className="page-feedback__reaction page-feedback__reaction--positive"
                        aria-label="This page was helpful"
                        aria-pressed={reaction === 'thumbsUp'}
                        disabled={isSaving}
                        onClick={() => selectReaction('thumbsUp')}
                    >
                        <ThumbIcon />
                    </button>
                    <button
                        type="button"
                        className="page-feedback__reaction page-feedback__reaction--negative"
                        aria-label="This page was not helpful"
                        aria-pressed={reaction === 'thumbsDown'}
                        disabled={isSaving}
                        onClick={() => selectReaction('thumbsDown')}
                    >
                        <ThumbIcon down />
                    </button>
                </div>
            </div>

            {showComment && (
                <div
                    className={`page-feedback__details${isClosing ? ' page-feedback__details--closing' : ''}`}
                >
                    <div className="page-feedback__details-inner">
                        <form
                            className={`page-feedback__form${isClosing ? ' page-feedback__form--closing' : ''}`}
                            onSubmit={submitComment}
                        >
                            <label
                                className="sr-only"
                                htmlFor={`${questionId}-comment`}
                            >
                                Tell us more (optional)
                            </label>
                            <textarea
                                ref={commentRef}
                                id={`${questionId}-comment`}
                                className="page-feedback__textarea"
                                value={comment}
                                maxLength={COMMENT_MAX_LENGTH}
                                placeholder="Tell us more (optional)"
                                aria-describedby={guidanceId}
                                onChange={(event) =>
                                    setComment(event.target.value)
                                }
                            />
                            <div className="page-feedback__form-footer">
                                <p
                                    id={guidanceId}
                                    className="page-feedback__guidance"
                                >
                                    Don&apos;t include passwords, API keys, or
                                    other sensitive information.
                                </p>
                                <span className="page-feedback__count">
                                    {comment.length}/{COMMENT_MAX_LENGTH}
                                </span>
                            </div>
                            <div className="page-feedback__actions">
                                {error && (
                                    <p
                                        className="page-feedback__error"
                                        role="alert"
                                    >
                                        We couldn&apos;t save your feedback.
                                    </p>
                                )}
                                <button
                                    type="submit"
                                    className="page-feedback__submit"
                                    disabled={isSaving || !comment.trim()}
                                >
                                    {isSaving
                                        ? 'Sending…'
                                        : error
                                          ? 'Try again'
                                          : 'Send feedback'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {showThanks && (
                <p className="page-feedback__thanks" role="status">
                    <span
                        className="page-feedback__thanks-icon"
                        aria-hidden="true"
                    >
                        ✓
                    </span>
                    Thanks for your feedback.
                </p>
            )}
        </section>
    )
}

customElements.define(
    'page-feedback',
    r2wc(PageFeedback, {
        props: {
            pageUrl: 'string',
            pageTitle: 'string',
        },
    })
)
