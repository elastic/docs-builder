// Pure logic for <docs-wizard>. No DOM, so it is easy to test.
// State is a replayable history: [{ id: <screen id>, picks: [<option index>, ...] }].

export interface Condition {
    key: string
    equals?: string
    starts_with?: string
    in?: string[]
}

export interface Option {
    label: string
    hint?: string
    tags?: string[]
    wide?: boolean
    next?: string
    exclusive?: boolean
    flag?: string
    set?: Record<string, string>
    add?: Record<string, number>
}

export interface Screen {
    type: 'question' | 'route' | 'result'
    progress: number
    layout?: 'list' | 'cards'
    question?: string
    why?: string
    multi?: boolean
    next?: string
    options?: Option[]
    rules?: { when: Condition; next: string }[]
    else?: string
}

export interface ResultItem {
    name: string
    blurb?: string
    docs: { title: string; href: string }
}

/** One thing the wizard decides. It comes from an answer, from scores, or both (an answer wins). */
export interface Variable {
    answer?: string
    score?: string[]
    close_call_within?: number
    /** Skip instead of failing when there is nothing to show. */
    optional?: boolean
    /** Only resolve when this holds. It can test answers or variables declared earlier. */
    when?: Condition
    items: Record<string, ResultItem>
}

export interface CtaRule {
    when?: Condition
    label: string
    href?: string
    href_from?: string
}

export interface WizardConfig {
    meta: {
        title: string
        intro?: string
        start_button?: string
        start: string
        progress_label?: string
    }
    screens: Record<string, Screen>
    results: {
        heading: string
        answers_heading?: string
        change_label?: string
        start_over_label?: string
        back_label?: string
        variables: Record<string, Variable>
        cta: CtaRule[]
        extra_docs?: { when: Condition; title: string; href: string }[]
        flag_notes?: Record<string, string>
        close_call_note?: string
    }
}

export interface HistoryEntry {
    id: string
    picks: number[]
}

/** Answers the reader gave before editing an earlier one, keyed by screen id. */
export type Remembered = Record<string, number[]>

export interface RecapEntry {
    index: number
    id: string
    question: string
    labels: string[]
}

export interface WizardState {
    current: string
    answers: Record<string, string>
    scores: Record<string, number>
    flags: string[]
    recap: RecapEntry[]
}

export interface ResolvedVariable {
    key: string
    item: ResultItem
    /** Set when a scored variable was a near tie: the runner-up. */
    closeCall?: ResultItem
}

export interface WizardResult {
    /** The chosen item key for each variable. */
    values: Record<string, string>
    heading: string
    blurbs: string[]
    cta: { label: string; href: string }
    docs: { title: string; href: string }[]
    notes: string[]
}

export function evalWhen(
    when: Condition,
    answers: Record<string, string>
): boolean {
    const value = answers[when.key]
    if (when.equals !== undefined) return value === when.equals
    if (when.starts_with !== undefined)
        return typeof value === 'string' && value.startsWith(when.starts_with)
    if (when.in !== undefined)
        return value !== undefined && when.in.includes(value)
    return false
}

/** Follows route screens until it reaches a question or the result. */
export function follow(
    config: WizardConfig,
    id: string,
    answers: Record<string, string>
): string {
    for (let guard = 0; guard < 25; guard++) {
        const screen = config.screens[id]
        if (!screen) throw new Error(`Unknown screen: ${id}`)
        if (screen.type !== 'route') return id
        const rule = (screen.rules ?? []).find((r) => evalWhen(r.when, answers))
        id = rule ? rule.next : (screen.else as string)
    }
    throw new Error(`Route loop at ${id}`)
}

/** Replays the history to get the current screen and everything derived from the answers. */
export function resolve(
    config: WizardConfig,
    history: HistoryEntry[]
): WizardState {
    const answers: Record<string, string> = {}
    const scores: Record<string, number> = {}
    const flags: string[] = []
    const recap: RecapEntry[] = []
    let current = follow(config, config.meta.start, answers)

    history.forEach((entry, index) => {
        const screen = config.screens[entry.id]
        const chosen = entry.picks.map((i) => {
            const option = screen.options?.[i]
            if (!option) throw new Error(`Bad option index ${i}`)
            return option
        })
        chosen.forEach((option) => {
            Object.assign(answers, option.set ?? {})
            Object.entries(option.add ?? {}).forEach(([key, n]) => {
                scores[key] = (scores[key] ?? 0) + n
            })
            if (option.flag) flags.push(option.flag)
        })
        recap.push({
            index,
            id: entry.id,
            question: screen.question ?? '',
            labels: chosen.map((o) => o.label),
        })
        const next = screen.multi ? screen.next : chosen[0].next
        current = follow(config, next as string, answers)
    })

    return { current, answers, scores, flags, recap }
}

function validPicks(screen: Screen, picks: number[] | undefined): boolean {
    if (!picks || picks.length === 0) return false
    if (!screen.multi && picks.length !== 1) return false
    return picks.every((i) => Number.isInteger(i) && !!screen.options?.[i])
}

/**
 * After an answer changes, re-applies the answers the reader already gave for the screens that come next, so editing one
 * answer doesn't make them answer everything again. It stops at the first question with no remembered answer, or at the result.
 */
export function replayRemembered(
    config: WizardConfig,
    history: HistoryEntry[],
    remembered: Remembered
): { history: HistoryEntry[]; remembered: Remembered } {
    const left = { ...remembered }
    const next = [...history]
    for (let guard = 0; guard < 100; guard++) {
        const { current } = resolve(config, next)
        const screen = config.screens[current]
        if (screen.type !== 'question' || !validPicks(screen, left[current]))
            break
        next.push({ id: current, picks: left[current] })
        delete left[current]
    }
    return { history: next, remembered: left }
}

/** Splits the history at `index` into the answers to keep and the ones to remember for later. */
export function startEdit(
    history: HistoryEntry[],
    index: number
): { history: HistoryEntry[]; remembered: Remembered } {
    const remembered: Remembered = {}
    history.slice(index).forEach((e) => {
        remembered[e.id] = e.picks
    })
    return { history: history.slice(0, index), remembered }
}

export function fill(template: string, vars: Record<string, string>): string {
    return template.replace(/\{(\w+)\}/g, (_, k: string) =>
        k in vars ? vars[k] : `{${k}}`
    )
}

/** Picks the chosen item for each variable: an answer wins, otherwise the highest score (ties go to the first listed). */
export function resolveVariables(
    config: WizardConfig,
    state: WizardState
): Record<string, ResolvedVariable> {
    const out: Record<string, ResolvedVariable> = {}
    // What a `when` can test: the answers, plus the variables resolved so far (in declaration order).
    const facts: Record<string, string> = { ...state.answers }

    for (const [name, variable] of Object.entries(config.results.variables)) {
        if (variable.when && !evalWhen(variable.when, facts)) continue

        const answered = variable.answer
            ? state.answers[variable.answer]
            : undefined
        let key = answered
        let closeCall: ResultItem | undefined

        if (!key) {
            const candidates = variable.score ?? []
            const ranked = candidates
                .map((candidate, order) => ({
                    candidate,
                    order,
                    score: state.scores[candidate] ?? 0,
                }))
                .sort((a, b) => b.score - a.score || a.order - b.order)
            key = ranked[0]?.candidate
            const within = variable.close_call_within ?? 1
            if (
                ranked.length > 1 &&
                ranked[0].score - ranked[1].score <= within
            ) {
                closeCall = variable.items[ranked[1].candidate]
            }
        }

        const item = key ? variable.items[key] : undefined
        if (!key || !item) {
            if (variable.optional) continue
            throw new Error(`No result for ${name} (${key ?? 'unset'})`)
        }
        out[name] = { key, item, closeCall }
        facts[name] = key
    }
    return out
}

/** Everything the result screen shows, derived from the config and the answers. */
export function buildResult(
    config: WizardConfig,
    state: WizardState
): WizardResult {
    const r = config.results
    const resolved = resolveVariables(config, state)
    const entries = Object.entries(resolved)
    const values = Object.fromEntries(entries.map(([n, v]) => [n, v.key]))
    const names = Object.fromEntries(entries.map(([n, v]) => [n, v.item.name]))
    // Conditions can test an answer or the item a variable resolved to.
    const facts = { ...state.answers, ...values }

    const rule = r.cta.find((c) => !c.when || evalWhen(c.when, facts))
    if (!rule) throw new Error('No results.cta rule matched')
    const href =
        rule.href ??
        (rule.href_from ? resolved[rule.href_from]?.item.docs.href : undefined)
    if (!href) throw new Error(`No link for CTA "${rule.label}"`)

    const docs = entries.map(([, v]) => v.item.docs)
    const extra = (r.extra_docs ?? []).find((e) => evalWhen(e.when, facts))
    if (extra) docs.push({ title: extra.title, href: extra.href })

    const notes = [...new Set(state.flags)]
        .map((flag) => r.flag_notes?.[flag])
        .filter((note): note is string => !!note)
    if (r.close_call_note) {
        for (const [, v] of entries) {
            if (v.closeCall) {
                notes.push(fill(r.close_call_note, { other: v.closeCall.name }))
            }
        }
    }

    return {
        values,
        heading: fill(r.heading, names),
        blurbs: entries
            .map(([, v]) => v.item.blurb)
            .filter((b): b is string => !!b),
        cta: { label: fill(rule.label, names), href },
        docs,
        notes,
    }
}
