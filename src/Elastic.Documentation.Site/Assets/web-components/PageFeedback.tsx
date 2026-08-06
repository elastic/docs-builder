import { config } from '../config'
import r2wc from '@r2wc/react-to-web-component'
import * as React from 'react'
import { FormEvent, useEffect, useId, useRef, useState } from 'react'

const COMMENT_MAX_LENGTH = 2000
const REASON_SET_VERSION = 1
const REACTION_SAVE_DELAY = 400

type Reaction = 'thumbsUp' | 'thumbsDown'
type Reason =
    | 'accurate'
    | 'solvedProblem'
    | 'easyToUnderstand'
    | 'helpfulExamples'
    | 'inaccurate'
    | 'missingInformation'
    | 'hardToUnderstand'
    | 'codeSampleErrors'
    | 'anotherReason'

interface ReasonOption {
    value: Reason
    label: string
    description?: string
}

const POSITIVE_REASONS: ReasonOption[] = [
    {
        value: 'accurate',
        label: 'Accurate',
        description: 'Accurately describes the product or feature.',
    },
    {
        value: 'solvedProblem',
        label: 'Solved my problem',
        description: 'Helped me resolve an issue.',
    },
    {
        value: 'easyToUnderstand',
        label: 'Easy to understand',
        description: 'Clear and easy to follow.',
    },
    {
        value: 'helpfulExamples',
        label: 'Helpful examples',
        description: 'The examples helped me complete my task.',
    },
    { value: 'anotherReason', label: 'Another reason' },
]

const NEGATIVE_REASONS: ReasonOption[] = [
    {
        value: 'inaccurate',
        label: 'Inaccurate',
        description: "Doesn't accurately describe the product or feature.",
    },
    {
        value: 'missingInformation',
        label: "Couldn't find what I needed",
        description: 'Missing important information.',
    },
    {
        value: 'hardToUnderstand',
        label: 'Hard to understand',
        description: 'Too complicated or unclear.',
    },
    {
        value: 'codeSampleErrors',
        label: 'Code sample errors',
        description: 'One or more code samples are incorrect.',
    },
    { value: 'anotherReason', label: 'Another reason' },
]

interface PageFeedbackProps {
    pageUrl: string
    pageTitle: string
}

interface PageFeedbackPayload {
    pageUrl: string
    pageTitle: string
    reaction: Reaction
    reason?: Reason
    reasonSetVersion?: number
    comment?: string
}

interface PendingReactionSave {
    finish: (shouldSave: boolean) => void
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

export const PageFeedback = ({ pageUrl, pageTitle }: PageFeedbackProps) => {
    const questionId = useId()
    const guidanceId = useId()
    const firstReasonRef = useRef<HTMLInputElement>(null)
    const initialSaveRef = useRef<Promise<void>>(Promise.resolve())
    const pendingReactionSaveRef = useRef<PendingReactionSave | null>(null)
    const [feedbackId] = useState(() => crypto.randomUUID())
    const [reaction, setReaction] = useState<Reaction | null>(null)
    const [reason, setReason] = useState<Reason | null>(null)
    const [comment, setComment] = useState('')
    const [isSaving, setIsSaving] = useState(false)
    const [showThanks, setShowThanks] = useState(false)
    const [error, setError] = useState(false)

    useEffect(() => {
        if (reaction) firstReasonRef.current?.focus()
    }, [reaction])

    useEffect(() => () => pendingReactionSaveRef.current?.finish(false), [])

    const saveInitialReaction = async (nextReaction: Reaction) => {
        const payload = { pageUrl, pageTitle, reaction: nextReaction }

        try {
            await submitFeedback(feedbackId, payload)
        } catch {
            try {
                await submitFeedback(feedbackId, payload)
            } catch {
                // The questionnaire remains available and its submission retries
                // the complete feedback payload.
            }
        }
    }

    const selectReaction = (nextReaction: Reaction) => {
        if (nextReaction === reaction) return

        setReaction(nextReaction)
        setReason(null)
        setError(false)
        pendingReactionSaveRef.current?.finish(false)

        const debounce = new Promise<boolean>((resolve) => {
            let settled = false
            const timeout = window.setTimeout(
                () => finish(true),
                REACTION_SAVE_DELAY
            )
            const finish = (shouldSave: boolean) => {
                if (settled) return

                settled = true
                window.clearTimeout(timeout)
                pendingReactionSaveRef.current = null
                resolve(shouldSave)
            }

            pendingReactionSaveRef.current = { finish }
        })

        initialSaveRef.current = initialSaveRef.current.then(async () => {
            if (await debounce) await saveInitialReaction(nextReaction)
        })
    }

    const submitDetails = async (event: FormEvent) => {
        event.preventDefault()
        const trimmedComment = comment.trim()
        if (!reaction || !reason || isSaving) return

        const payload: PageFeedbackPayload = {
            pageUrl,
            pageTitle,
            reaction,
            reason,
            reasonSetVersion: REASON_SET_VERSION,
            ...(trimmedComment ? { comment: trimmedComment } : {}),
        }

        setIsSaving(true)
        setError(false)

        try {
            pendingReactionSaveRef.current?.finish(true)
            await initialSaveRef.current
            await submitFeedback(feedbackId, payload)
            setShowThanks(true)
        } catch {
            setError(true)
        } finally {
            setIsSaving(false)
        }
    }

    if (showThanks) {
        return (
            <section className="page-feedback page-feedback--success">
                <p className="page-feedback__thanks" role="status">
                    <span
                        className="page-feedback__thanks-icon"
                        aria-hidden="true"
                    >
                        ✓
                    </span>
                    Thank you for your feedback.
                </p>
            </section>
        )
    }

    return (
        <section className="page-feedback">
            <div className="page-feedback__prompt">
                <p id={questionId} className="page-feedback__question">
                    Was this page helpful?
                </p>
                <div
                    className="page-feedback__choices"
                    role="group"
                    aria-labelledby={questionId}
                >
                    <button
                        type="button"
                        className="page-feedback__choice page-feedback__choice--yes"
                        aria-label="Yes, this page was helpful"
                        aria-pressed={reaction === 'thumbsUp'}
                        disabled={isSaving}
                        onClick={() => selectReaction('thumbsUp')}
                    >
                        <ThumbIcon />
                        Yes
                    </button>
                    <button
                        type="button"
                        className="page-feedback__choice page-feedback__choice--no"
                        aria-label="No, this page was not helpful"
                        aria-pressed={reaction === 'thumbsDown'}
                        disabled={isSaving}
                        onClick={() => selectReaction('thumbsDown')}
                    >
                        <ThumbIcon down />
                        No
                    </button>
                </div>
            </div>

            {reaction && (
                <form className="page-feedback__form" onSubmit={submitDetails}>
                    <fieldset
                        className="page-feedback__reasons"
                        disabled={isSaving}
                    >
                        <legend className="page-feedback__legend">
                            {reaction === 'thumbsUp'
                                ? 'What did you like?'
                                : 'What went wrong?'}
                        </legend>
                        {(reaction === 'thumbsUp'
                            ? POSITIVE_REASONS
                            : NEGATIVE_REASONS
                        ).map((option, index) => (
                            <label
                                key={option.value}
                                className="page-feedback__reason"
                            >
                                <input
                                    ref={
                                        index === 0 ? firstReasonRef : undefined
                                    }
                                    type="radio"
                                    name={`${questionId}-reason`}
                                    value={option.value}
                                    checked={reason === option.value}
                                    onChange={() => {
                                        setReason(option.value)
                                        setError(false)
                                    }}
                                />
                                <span>
                                    <span className="page-feedback__reason-label">
                                        {option.label}
                                    </span>
                                    {option.description && (
                                        <span className="page-feedback__reason-description">
                                            {option.description}
                                        </span>
                                    )}
                                </span>
                            </label>
                        ))}
                    </fieldset>

                    {reason && (
                        <div className="page-feedback__details">
                            <label
                                className="sr-only"
                                htmlFor={`${questionId}-comment`}
                            >
                                Tell us more (optional)
                            </label>
                            <textarea
                                id={`${questionId}-comment`}
                                className="page-feedback__textarea"
                                value={comment}
                                maxLength={COMMENT_MAX_LENGTH}
                                placeholder="Tell us more (optional)"
                                aria-describedby={guidanceId}
                                disabled={isSaving}
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
                        </div>
                    )}

                    <div className="page-feedback__actions">
                        <button
                            type="submit"
                            className="page-feedback__submit"
                            disabled={isSaving || !reason}
                        >
                            {isSaving
                                ? 'Sending…'
                                : error
                                  ? 'Try again'
                                  : 'Submit'}
                        </button>
                        {error && (
                            <p className="page-feedback__error" role="alert">
                                We couldn&apos;t save your feedback.
                            </p>
                        )}
                    </div>
                </form>
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
