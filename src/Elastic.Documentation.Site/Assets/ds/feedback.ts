// Page feedback: a reaction, then reasons and an optional comment. The server renders the whole widget, hidden;
// this module reveals it and sends the feedback. Hooks are data attributes, never styling classes.
const commentMaxLength = 2000
const reactionSaveDelay = 400
const draftPrefix = 'docs-page-feedback:'

type Reaction = 'thumbsUp' | 'thumbsDown'

interface Draft {
    feedbackId: string
    reaction: Reaction
    reasons: string[]
    comment: string
}

const draftKey = (pageUrl: string) => `${draftPrefix}${pageUrl}`

function readDraft(pageUrl: string, known: Set<string>): Draft | null {
    try {
        const raw = sessionStorage.getItem(draftKey(pageUrl))
        if (!raw) return null
        const parsed = JSON.parse(raw) as Partial<Draft>
        if (
            typeof parsed.feedbackId !== 'string' ||
            (parsed.reaction !== 'thumbsUp' && parsed.reaction !== 'thumbsDown')
        ) {
            return null
        }
        return {
            feedbackId: parsed.feedbackId,
            reaction: parsed.reaction,
            reasons: Array.isArray(parsed.reasons)
                ? parsed.reasons.filter((r) => known.has(r))
                : [],
            comment:
                typeof parsed.comment === 'string'
                    ? parsed.comment.slice(0, commentMaxLength)
                    : '',
        }
    } catch {
        return null
    }
}

function writeDraft(pageUrl: string, draft: Draft) {
    try {
        sessionStorage.setItem(draftKey(pageUrl), JSON.stringify(draft))
    } catch {
        // private mode or quota: the draft only survives this page view
    }
}

function clearDraft(pageUrl: string) {
    try {
        sessionStorage.removeItem(draftKey(pageUrl))
    } catch {
        // private mode
    }
}

class PageFeedback {
    private readonly pageUrl: string
    private readonly pageTitle: string
    private readonly api: string
    private readonly reasonVersion: number
    private readonly form: HTMLFormElement
    private readonly submit: HTMLButtonElement
    private readonly comment: HTMLTextAreaElement
    private readonly count: HTMLElement
    private readonly error: HTMLElement
    private readonly thanks: HTMLElement
    private readonly buttons: HTMLButtonElement[]
    private readonly feedbackId: string
    private reaction: Reaction | null = null
    private saving = false
    private initialSave: Promise<void> = Promise.resolve()
    private cancelPending: ((shouldSave: boolean) => void) | null = null

    constructor(private readonly root: HTMLElement) {
        this.pageUrl = root.dataset.feedbackPageUrl ?? ''
        this.pageTitle = root.dataset.feedbackPageTitle ?? ''
        this.api = root.dataset.feedbackApi ?? ''
        this.reasonVersion = Number(root.dataset.feedbackReasonVersion ?? 1)
        this.form = root.querySelector<HTMLFormElement>('[data-feedback-form]')!
        this.submit = root.querySelector<HTMLButtonElement>(
            '[data-feedback-submit]'
        )!
        this.comment = root.querySelector<HTMLTextAreaElement>(
            '[data-feedback-comment]'
        )!
        this.count = root.querySelector<HTMLElement>('[data-feedback-count]')!
        this.error = root.querySelector<HTMLElement>('[data-feedback-error]')!
        this.thanks = root.querySelector<HTMLElement>('[data-feedback-thanks]')!
        this.buttons = Array.from(
            root.querySelectorAll<HTMLButtonElement>('[data-feedback-reaction]')
        )

        const known = new Set(
            Array.from(
                root.querySelectorAll<HTMLInputElement>(
                    '[data-feedback-reason]'
                )
            ).map((i) => i.value)
        )
        const draft = readDraft(this.pageUrl, known)
        this.feedbackId = draft?.feedbackId ?? crypto.randomUUID()

        this.buttons.forEach((button) =>
            button.addEventListener('click', () =>
                this.select(button.dataset.feedbackReaction as Reaction, true)
            )
        )
        this.form.addEventListener('change', () => this.changed())
        this.comment.addEventListener('input', () => this.changed())
        this.form.addEventListener('submit', (event) => {
            event.preventDefault()
            void this.send()
        })

        root.hidden = false
        if (draft) {
            this.select(draft.reaction, false)
            this.comment.value = draft.comment
            this.checkboxes().forEach(
                (box) => (box.checked = draft.reasons.includes(box.value))
            )
            this.changed()
        }
    }

    private visibleSet(): HTMLElement | null {
        return this.form.querySelector<HTMLElement>(
            `[data-feedback-reasons="${this.reaction}"]`
        )
    }

    private checkboxes(): HTMLInputElement[] {
        return Array.from(
            this.form.querySelectorAll<HTMLInputElement>(
                '[data-feedback-reason]'
            )
        )
    }

    private selected(): string[] {
        const set = this.visibleSet()
        return set
            ? Array.from(
                  set.querySelectorAll<HTMLInputElement>(
                      '[data-feedback-reason]:checked'
                  )
              ).map((b) => b.value)
            : []
    }

    private select(next: Reaction, userAction: boolean) {
        if (next === this.reaction) return
        this.reaction = next
        this.error.hidden = true
        this.buttons.forEach((b) =>
            b.setAttribute(
                'aria-pressed',
                String(b.dataset.feedbackReaction === next)
            )
        )
        this.form.hidden = false
        this.form
            .querySelectorAll<HTMLElement>('[data-feedback-reasons]')
            .forEach((set) => {
                const active = set.dataset.feedbackReasons === next
                set.hidden = !active
                // Reasons from the other set no longer apply.
                if (!active)
                    set.querySelectorAll<HTMLInputElement>('input').forEach(
                        (i) => (i.checked = false)
                    )
            })
        this.changed()

        if (userAction) {
            this.visibleSet()?.querySelector<HTMLInputElement>('input')?.focus()
            this.saveReactionSoon(next)
        }
    }

    // The reaction alone is worth keeping, so it saves after a short pause; a quick change of mind saves only the last one.
    private saveReactionSoon(reaction: Reaction) {
        this.cancelPending?.(false)
        const decision = new Promise<boolean>((resolve) => {
            let settled = false
            const finish = (shouldSave: boolean) => {
                if (settled) return
                settled = true
                window.clearTimeout(timer)
                this.cancelPending = null
                resolve(shouldSave)
            }
            const timer = window.setTimeout(
                () => finish(true),
                reactionSaveDelay
            )
            this.cancelPending = finish
        })
        this.initialSave = this.initialSave.then(async () => {
            if (!(await decision)) return
            try {
                await this.put({
                    pageUrl: this.pageUrl,
                    pageTitle: this.pageTitle,
                    reaction,
                })
            } catch {
                // the questionnaire submit retries the complete payload
            }
        })
    }

    private changed() {
        const text = this.comment.value
        this.count.textContent = `${text.length}/${commentMaxLength}`
        this.error.hidden = true
        this.submit.disabled = this.saving || this.selected().length === 0
        if (this.reaction) {
            writeDraft(this.pageUrl, {
                feedbackId: this.feedbackId,
                reaction: this.reaction,
                reasons: this.selected(),
                comment: text,
            })
        }
    }

    private async put(payload: Record<string, unknown>) {
        const response = await fetch(`${this.api}/${this.feedbackId}`, {
            method: 'PUT',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
        })
        if (!response.ok)
            throw new Error(
                `Feedback request failed with status ${response.status}`
            )
    }

    private async send() {
        const reasons = this.selected()
        if (!this.reaction || reasons.length === 0 || this.saving) return
        const comment = this.comment.value.trim()

        this.saving = true
        this.submit.disabled = true
        this.submit.textContent = 'Sending…'
        this.error.hidden = true
        try {
            this.cancelPending?.(false)
            await this.initialSave
            await this.put({
                pageUrl: this.pageUrl,
                pageTitle: this.pageTitle,
                reaction: this.reaction,
                reasons,
                reasonSetVersion: this.reasonVersion,
                ...(comment ? { comment } : {}),
            })
            clearDraft(this.pageUrl)
            this.root
                .querySelectorAll<HTMLElement>(
                    '[data-feedback-prompt], [data-feedback-form]'
                )
                .forEach((el) => (el.hidden = true))
            this.thanks.hidden = false
        } catch {
            this.error.hidden = false
            this.submit.textContent = 'Try again'
        } finally {
            this.saving = false
            this.submit.disabled = this.selected().length === 0
        }
    }
}

export function initFeedback(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>(
            '[data-feedback]:not([data-feedback-bound])'
        )
        .forEach((root) => {
            root.dataset.feedbackBound = ''
            new PageFeedback(root)
        })
}
