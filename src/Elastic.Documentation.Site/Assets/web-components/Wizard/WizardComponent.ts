import {
    buildResult,
    replayRemembered,
    resolve,
    startEdit,
    type HistoryEntry,
    type Remembered,
    type WizardConfig,
    type WizardState,
} from './engine'
import { styles } from './styles'

type Child = Node | string | null | undefined | false

/** Builds elements with textContent only, so config copy can never inject markup. */
function h(
    tag: string,
    attrs: Record<string, string | boolean | EventListener | undefined> = {},
    ...kids: (Child | Child[])[]
): HTMLElement {
    const el = document.createElement(tag)
    for (const [key, value] of Object.entries(attrs)) {
        if (value === undefined || value === false) continue
        if (key.startsWith('on') && typeof value === 'function') {
            el.addEventListener(key.slice(2), value)
        } else if (key === 'class') {
            el.className = String(value)
        } else {
            el.setAttribute(key, String(value))
        }
    }
    for (const kid of kids.flat()) {
        if (kid) el.append(kid)
    }
    return el
}

/** Config links are author-controlled, but never let one become a script URL. */
function safeHref(href: string): string {
    return /^(https?:\/\/|\/)/i.test(href) ? href : '#'
}

class DocsWizard extends HTMLElement {
    private config: WizardConfig | null = null
    private history: HistoryEntry[] = []
    /** Answers from before an edit, re-applied while they still fit. */
    private remembered: Remembered = {}
    private started = false
    private multiPicks = new Set<number>()
    private root: ShadowRoot

    constructor() {
        super()
        this.root = this.attachShadow({ mode: 'open' })
    }

    connectedCallback() {
        const script = this.querySelector<HTMLScriptElement>(
            'script[data-wizard-config]'
        )
        try {
            this.config = JSON.parse(script?.textContent ?? '') as WizardConfig
        } catch {
            this.showError('The wizard config could not be read.')
            return
        }
        this.render()
    }

    private mount(...nodes: (Child | Child[])[]) {
        const card = h(
            'div',
            { class: 'card', 'aria-live': 'polite' },
            nodes.flat()
        )
        this.root.replaceChildren(h('style', {}, styles), card)
        const title = card.querySelector<HTMLElement>('h2.title')
        title?.setAttribute('tabindex', '-1')
        title?.focus({ preventScroll: true })
    }

    private showError(message: string) {
        this.root.replaceChildren(
            h('style', {}, styles),
            h('div', { class: 'card error', role: 'alert' }, message)
        )
    }

    private render() {
        const config = this.config
        if (!config) return
        if (!this.started) return this.renderIntro(config)
        try {
            const state = resolve(config, this.history)
            if (config.screens[state.current].type === 'result') {
                this.renderResult(config, state)
            } else {
                this.renderQuestion(config, state)
            }
        } catch (error) {
            this.showError(
                error instanceof Error ? error.message : 'Something went wrong.'
            )
        }
    }

    private progress(config: WizardConfig, pct: number, right: string): Node[] {
        const label = config.meta.progress_label ?? 'Your progress'
        return [
            h(
                'div',
                {
                    class: 'progress',
                    role: 'progressbar',
                    'aria-valuemin': '0',
                    'aria-valuemax': '100',
                    'aria-valuenow': String(pct),
                    'aria-label': label,
                },
                h('div', { style: `width:${pct}%` })
            ),
            h(
                'div',
                { class: 'progress-label' },
                h('span', {}, label),
                h('span', {}, right)
            ),
        ]
    }

    private topbar(config: WizardConfig): Node {
        const r = config.results
        return h(
            'div',
            { class: 'topbar' },
            h(
                'button',
                { class: 'link', type: 'button', onclick: () => this.back() },
                `← ${r.back_label ?? 'Back'}`
            ),
            h(
                'button',
                { class: 'link', type: 'button', onclick: () => this.reset() },
                r.start_over_label ?? 'Start over'
            )
        )
    }

    private back() {
        if (this.history.length) this.history.pop()
        else this.started = false
        this.remembered = {}
        this.multiPicks = new Set()
        this.render()
    }

    private reset() {
        this.history = []
        this.remembered = {}
        this.started = false
        this.multiPicks = new Set()
        this.render()
    }

    /** Edit one answer: keep the ones before it, remember the ones after it so they can be re-applied. */
    private change(index: number) {
        const split = startEdit(this.history, index)
        this.history = split.history
        this.remembered = split.remembered
        this.prepareMultiPicks()
        this.render()
    }

    private choose(id: string, picks: number[]) {
        this.history.push({ id, picks })
        delete this.remembered[id]
        if (this.config) {
            const replayed = replayRemembered(
                this.config,
                this.history,
                this.remembered
            )
            this.history = replayed.history
            this.remembered = replayed.remembered
        }
        this.multiPicks = new Set()
        this.prepareMultiPicks()
        this.render()
    }

    /** When editing a multi-select screen, start from what the reader picked before. */
    private prepareMultiPicks() {
        const config = this.config
        if (!config) return
        const { current } = resolve(config, this.history)
        const previous = this.remembered[current]
        this.multiPicks = new Set(
            config.screens[current].multi && previous ? previous : []
        )
    }

    private renderIntro(config: WizardConfig) {
        this.mount(
            h(
                'div',
                { class: 'intro' },
                h('h2', { class: 'title' }, config.meta.title),
                config.meta.intro ? h('p', {}, config.meta.intro) : null,
                h(
                    'button',
                    {
                        class: 'btn',
                        type: 'button',
                        onclick: () => {
                            this.started = true
                            this.render()
                        },
                    },
                    config.meta.start_button ?? 'Start'
                )
            )
        )
    }

    private renderQuestion(config: WizardConfig, state: WizardState) {
        const id = state.current
        const screen = config.screens[id]
        const options = screen.options ?? []
        const previous = this.remembered[id] ?? []
        const list = h('ul', {
            class: screen.layout === 'cards' ? 'options cards' : 'options',
        })
        options.forEach((option, i) => {
            const wasPicked = previous.includes(i)
            const body = h(
                'span',
                {},
                h('span', { class: 'label' }, option.label),
                option.hint ? h('span', { class: 'hint' }, option.hint) : null,
                option.tags?.length
                    ? h(
                          'span',
                          { class: 'tags' },
                          option.tags.map((tag) =>
                              h('span', { class: 'tag' }, tag)
                          )
                      )
                    : null,
                wasPicked
                    ? h('span', { class: 'was' }, 'Your previous answer')
                    : null
            )
            const button = screen.multi
                ? h(
                      'button',
                      {
                          class: 'option',
                          type: 'button',
                          'data-previous': wasPicked,
                          'aria-pressed': String(this.multiPicks.has(i)),
                          onclick: () => this.toggle(options, i),
                      },
                      h('span', { class: 'box' }),
                      body
                  )
                : h(
                      'button',
                      {
                          class: 'option',
                          type: 'button',
                          'data-previous': wasPicked,
                          onclick: () => this.choose(id, [i]),
                      },
                      body
                  )
            list.append(
                h('li', { class: option.wide ? 'wide' : undefined }, button)
            )
        })

        const nodes: (Child | Child[])[] = [
            ...this.progress(
                config,
                screen.progress,
                `Question ${state.recap.length + 1}`
            ),
            this.topbar(config),
            h('h2', { class: 'title' }, screen.question),
            screen.why
                ? h(
                      'div',
                      { class: 'why' },
                      h('b', {}, 'Why we ask: '),
                      screen.why
                  )
                : null,
            list,
        ]
        if (screen.multi) {
            nodes.push(
                h(
                    'div',
                    { class: 'actions' },
                    h(
                        'button',
                        {
                            class: 'btn',
                            type: 'button',
                            'data-continue': 'true',
                            disabled: this.multiPicks.size === 0,
                            onclick: () =>
                                this.choose(
                                    id,
                                    [...this.multiPicks].sort((a, b) => a - b)
                                ),
                        },
                        'Continue'
                    ),
                    h('span', { class: 'hint-text' }, 'Select all that apply')
                )
            )
        }
        this.mount(...nodes)
    }

    private toggle(
        options: NonNullable<WizardConfig['screens'][string]['options']>,
        i: number
    ) {
        if (this.multiPicks.has(i)) {
            this.multiPicks.delete(i)
        } else {
            if (options[i].exclusive) this.multiPicks.clear()
            else
                options.forEach(
                    (o, j) => o.exclusive && this.multiPicks.delete(j)
                )
            this.multiPicks.add(i)
        }
        this.root.querySelectorAll('.option').forEach((button, j) => {
            button.setAttribute('aria-pressed', String(this.multiPicks.has(j)))
        })
        const next =
            this.root.querySelector<HTMLButtonElement>('[data-continue]')
        if (next) next.disabled = this.multiPicks.size === 0
    }

    private renderResult(config: WizardConfig, state: WizardState) {
        const result = buildResult(config, state)
        const r = config.results
        this.mount(
            ...this.progress(config, 100, 'Done'),
            this.topbar(config),
            h('span', { class: 'badge' }, 'Your match'),
            h('h2', { class: 'title' }, result.heading),
            h(
                'div',
                { class: 'blurbs' },
                result.blurbs.map((b) => h('p', {}, b))
            ),
            h(
                'div',
                { class: 'actions' },
                h(
                    'a',
                    {
                        class: 'btn',
                        href: safeHref(result.cta.href),
                        target: '_blank',
                        rel: 'noopener',
                    },
                    result.cta.label
                )
            ),
            result.notes.map((note) => h('div', { class: 'note' }, note)),
            h('h3', {}, 'Next steps'),
            h(
                'div',
                { class: 'docs' },
                result.docs.map((doc) =>
                    h(
                        'a',
                        {
                            class: 'doc',
                            href: safeHref(doc.href),
                            target: '_blank',
                            rel: 'noopener',
                        },
                        h('small', {}, 'Docs'),
                        doc.title
                    )
                )
            ),
            h(
                'details',
                { class: 'answers' },
                h(
                    'summary',
                    {},
                    `${r.answers_heading ?? 'Your answers'} (${state.recap.length})`
                ),
                h(
                    'ul',
                    { class: 'recap' },
                    state.recap.map((entry) =>
                        h(
                            'li',
                            {},
                            h(
                                'span',
                                {},
                                h('span', { class: 'q' }, entry.question),
                                h('br'),
                                h(
                                    'span',
                                    { class: 'a' },
                                    entry.labels.join(', ')
                                )
                            ),
                            h(
                                'button',
                                {
                                    class: 'link',
                                    type: 'button',
                                    onclick: () => this.change(entry.index),
                                },
                                r.change_label ?? 'Change'
                            )
                        )
                    )
                )
            ),
            h(
                'div',
                { class: 'actions' },
                h(
                    'button',
                    {
                        class: 'link',
                        type: 'button',
                        onclick: () => this.reset(),
                    },
                    r.start_over_label ?? 'Start over'
                )
            )
        )
    }
}

if (!customElements.get('docs-wizard')) {
    customElements.define('docs-wizard', DocsWizard)
}

export {}
