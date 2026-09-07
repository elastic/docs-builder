import { config } from '../config'
import r2wc from '@r2wc/react-to-web-component'
import * as React from 'react'
import { FormEvent, useEffect, useId, useRef, useState } from 'react'

const COMMENT_MAX_LENGTH = 2000
const REASON_SET_VERSION = 1
const REACTION_SAVE_DELAY = 400
const DRAFT_STORAGE_PREFIX = 'docs-page-feedback:'

type Reaction = 'thumbsUp' | 'thumbsDown'
type Surface = 'docs' | 'api'
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

const API_COPY: Partial<Record<Reason, Partial<ReasonOption>>> = {
    accurate: { description: 'Matches the real API.' },
    solvedProblem: {
        description: 'Helped me call the endpoint or use the schema.',
    },
    easyToUnderstand: {
        description: 'Parameters, types, or errors were clear.',
    },
    helpfulExamples: {
        description: 'The request or response examples helped.',
    },
    inaccurate: { description: 'Does not match the real API.' },
    missingInformation: {
        description: 'Missing a parameter, field, status, or auth detail.',
    },
    codeSampleErrors: {
        label: 'Example errors',
        description: 'A request or response example is wrong.',
    },
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

const reasonsFor = (surface: Surface, options: ReasonOption[]) =>
    surface === 'api'
        ? options.map((option) => ({ ...option, ...API_COPY[option.value] }))
        : options

const REASON_VALUES = new Set<Reason>([
    ...POSITIVE_REASONS.map((option) => option.value),
    ...NEGATIVE_REASONS.map((option) => option.value),
])

interface FeedbackDraft {
    feedbackId: string
    reaction: Reaction
    reason: Reason | null
    comments: Partial<Record<Reason, string>>
}

const draftKey = (pageUrl: string) => `${DRAFT_STORAGE_PREFIX}${pageUrl}`

const readDraft = (pageUrl: string): FeedbackDraft | null => {
    try {
        const raw = sessionStorage.getItem(draftKey(pageUrl))
        if (!raw) return null

        const parsed = JSON.parse(raw) as Partial<FeedbackDraft>
        if (
            typeof parsed.feedbackId !== 'string' ||
            (parsed.reaction !== 'thumbsUp' && parsed.reaction !== 'thumbsDown')
        ) {
            return null
        }

        const comments: Partial<Record<Reason, string>> = {}
        if (parsed.comments && typeof parsed.comments === 'object') {
            for (const [value, comment] of Object.entries(parsed.comments)) {
                if (
                    REASON_VALUES.has(value as Reason) &&
                    typeof comment === 'string'
                ) {
                    comments[value as Reason] = comment.slice(
                        0,
                        COMMENT_MAX_LENGTH
                    )
                }
            }
        }

        return {
            feedbackId: parsed.feedbackId,
            reaction: parsed.reaction,
            reason:
                parsed.reason && REASON_VALUES.has(parsed.reason)
                    ? parsed.reason
                    : null,
            comments,
        }
    } catch {
        return null
    }
}

const writeDraft = (pageUrl: string, draft: FeedbackDraft) => {
    try {
        sessionStorage.setItem(draftKey(pageUrl), JSON.stringify(draft))
    } catch {
        // Private mode or quota.
    }
}

const clearDraft = (pageUrl: string) => {
    try {
        sessionStorage.removeItem(draftKey(pageUrl))
    } catch {
        // Private mode.
    }
}

interface PageFeedbackProps {
    pageUrl: string
    pageTitle: string
    surface?: Surface
}

interface PageFeedbackPayload {
    pageUrl: string
    pageTitle: string
    reaction: Reaction
    reason?: Reason
    reasonSetVersion?: number
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

export const PageFeedback = ({
    pageUrl,
    pageTitle,
    surface = 'docs',
}: PageFeedbackProps) => {
    const questionId = useId()
    const guidanceId = useId()
    const firstReasonRef = useRef<HTMLInputElement>(null)
    const initialSaveRef = useRef<Promise<void>>(Promise.resolve())
    const pendingReactionSaveRef = useRef<
        ((shouldSave: boolean) => void) | null
    >(null)
    const [draft] = useState(() => readDraft(pageUrl))
    const [feedbackId] = useState(
        () => draft?.feedbackId ?? crypto.randomUUID()
    )
    const [reaction, setReaction] = useState<Reaction | null>(
        () => draft?.reaction ?? null
    )
    const [reason, setReason] = useState<Reason | null>(
        () => draft?.reason ?? null
    )
    const [comments, setComments] = useState<Partial<Record<Reason, string>>>(
        () => draft?.comments ?? {}
    )
    const [isSaving, setIsSaving] = useState(false)
    const [showThanks, setShowThanks] = useState(false)
    const [error, setError] = useState(false)
    const skipInitialFocus = useRef(Boolean(draft?.reaction))

    useEffect(() => {
        if (!reaction) return
        if (skipInitialFocus.current) {
            skipInitialFocus.current = false
            return
        }
        firstReasonRef.current?.focus()
    }, [reaction])

    useEffect(() => {
        if (showThanks || !reaction) return
        writeDraft(pageUrl, { feedbackId, reaction, reason, comments })
    }, [pageUrl, feedbackId, reaction, reason, comments, showThanks])

    useEffect(() => () => pendingReactionSaveRef.current?.(false), [])

    const saveInitialReaction = async (nextReaction: Reaction) => {
        try {
            await submitFeedback(feedbackId, {
                pageUrl,
                pageTitle,
                reaction: nextReaction,
            })
        } catch {
            // Questionnaire submit retries the complete payload.
        }
    }

    const selectReaction = (nextReaction: Reaction) => {
        if (nextReaction === reaction) return

        setReaction(nextReaction)
        setReason(
            reason &&
                (nextReaction === 'thumbsUp'
                    ? POSITIVE_REASONS
                    : NEGATIVE_REASONS
                ).some((option) => option.value === reason)
                ? reason
                : null
        )
        setError(false)
        pendingReactionSaveRef.current?.(false)

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

            pendingReactionSaveRef.current = finish
        })

        initialSaveRef.current = initialSaveRef.current.then(async () => {
            if (await debounce) await saveInitialReaction(nextReaction)
        })
    }

    const submitDetails = async (event: FormEvent) => {
        event.preventDefault()
        if (!reaction || !reason || isSaving) return
        const trimmedComment = (comments[reason] ?? '').trim()

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
            pendingReactionSaveRef.current?.(false)
            await initialSaveRef.current
            await submitFeedback(feedbackId, payload)
            clearDraft(pageUrl)
            setShowThanks(true)
        } catch {
            setError(true)
        } finally {
            setIsSaving(false)
        }
    }

    if (showThanks) {
        return (
            <section className="page-feedback">
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
                        {reasonsFor(
                            surface,
                            reaction === 'thumbsUp'
                                ? POSITIVE_REASONS
                                : NEGATIVE_REASONS
                        ).map((option, index) => {
                            const optionComment = comments[option.value] ?? ''
                            return (
                                <div
                                    key={option.value}
                                    className="page-feedback__option"
                                >
                                    <label className="page-feedback__reason">
                                        <input
                                            ref={
                                                index === 0
                                                    ? firstReasonRef
                                                    : undefined
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
                                    {reason === option.value && (
                                        <div className="page-feedback__details">
                                            <label
                                                className="sr-only"
                                                htmlFor={`${questionId}-comment-${option.value}`}
                                            >
                                                Tell us more (optional)
                                            </label>
                                            <textarea
                                                id={`${questionId}-comment-${option.value}`}
                                                className="page-feedback__textarea"
                                                value={optionComment}
                                                maxLength={COMMENT_MAX_LENGTH}
                                                placeholder="Tell us more (optional)"
                                                aria-describedby={guidanceId}
                                                disabled={isSaving}
                                                onChange={(event) =>
                                                    setComments((current) => ({
                                                        ...current,
                                                        [option.value]:
                                                            event.target.value,
                                                    }))
                                                }
                                            />
                                            <div className="page-feedback__form-footer">
                                                <p
                                                    id={guidanceId}
                                                    className="page-feedback__guidance"
                                                >
                                                    Don&apos;t include
                                                    passwords, API keys, or
                                                    other sensitive information.
                                                </p>
                                                <span className="page-feedback__count">
                                                    {optionComment.length}/
                                                    {COMMENT_MAX_LENGTH}
                                                </span>
                                            </div>
                                        </div>
                                    )}
                                </div>
                            )
                        })}
                    </fieldset>

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
            surface: 'string',
        },
    })
)
