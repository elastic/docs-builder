/**
 * Examples rail on API operation pages: example chips pick a scenario, each scenario shows a
 * scroll-snap carousel with one card per language. Console is the default language; an explicit pick is remembered across pages.
 */
import { iconCheckEui, iconCopyEui, temporarilyChangeIcon } from './copybutton'
import { closeIcon, fullscreenIcon } from './icons'
import { prefersReducedMotion } from './motion'
import { lockPageScroll } from './scroll-lock'
import { flashTooltip, initTooltips } from './tooltip'

export const apiLanguageStorageKey = 'api-language'
const defaultLanguage = 'Console'

let delegated = false

/** Language the reader picked last. */
function savedApiLanguage(): string | null {
    try {
        return window.localStorage.getItem(apiLanguageStorageKey)
    } catch {
        return null
    }
}

function saveApiLanguage(language: string): void {
    try {
        window.localStorage.setItem(apiLanguageStorageKey, language)
    } catch {
        // Storage can be disabled; the choice then lasts for this page only.
    }
}

/**
 * `#example=search-slicing&lang=java`, ignored when the hash is a plain anchor. Written in lowercase, and read
 * without regard to case so a hand-typed `lang=Java` still works.
 */
function readDeepLink(): { lang?: string; example?: string } {
    const hash = window.location.hash.slice(1)
    if (!/(^|&)(lang|example)=/.test(hash)) return {}
    const params = new URLSearchParams(hash)
    return {
        lang: params.get('lang')?.toLowerCase(),
        example: params.get('example')?.toLowerCase(),
    }
}

function writeDeepLink(lang: string | undefined, example: string | undefined) {
    const params = new URLSearchParams()
    if (example) params.set('example', example.toLowerCase())
    if (lang) params.set('lang', lang.toLowerCase())
    window.history.replaceState(null, '', `#${params.toString()}`)
}

function sameLanguage(a: string | undefined, b: string | undefined) {
    return !!a && !!b && a.toLowerCase() === b.toLowerCase()
}

/**
 * The strip is as tall as the active snippet: its header plus its code, capped at the max-lines height.
 * Measured from the code (scrollHeight is the full content even when clipped), so it does not depend on how
 * much room the rail currently gives the card.
 */
function syncStripHeight(carousel: HTMLElement) {
    const strip = carousel.querySelector<HTMLElement>('[data-carousel-strip]')
    const card = carousel.querySelector<HTMLElement>(
        '.api-code-carousel-card.is-active'
    )
    const body = card?.querySelector<HTMLElement>(
        '.api-code-carousel-card-body'
    )
    if (!strip || !card || !body) return
    const cap = parseFloat(getComputedStyle(body).maxHeight)
    const code = Number.isFinite(cap)
        ? Math.min(body.scrollHeight, cap)
        : body.scrollHeight
    const header = card.querySelector<HTMLElement>('header')?.offsetHeight ?? 0
    // The card's border and the strip's own padding, or the card comes up short and its code scrolls.
    const stripStyle = getComputedStyle(strip)
    const chrome =
        2 +
        (parseFloat(stripStyle.paddingTop) || 0) +
        (parseFloat(stripStyle.paddingBottom) || 0)
    const height = header + code + chrome
    if (code > 0) strip.style.setProperty('--api-strip-height', `${height}px`)
}

/** Offers "Show more" only when the description is cut off by its three-line clamp. */
function syncDescription(examples: HTMLElement) {
    const description = visiblePanel(examples)?.querySelector<HTMLElement>(
        '[data-example-description]'
    )
    const text = description?.querySelector<HTMLElement>(
        '.api-example-description-text'
    )
    const toggle = description?.querySelector<HTMLElement>(
        '[data-description-toggle]'
    )
    if (!description || !text || !toggle) return
    const open = description.classList.contains('is-open')
    toggle.hidden = !open && text.scrollHeight <= text.clientHeight + 1
}

function toggleDescription(button: HTMLElement) {
    const description = button.closest<HTMLElement>(
        '[data-example-description]'
    )
    if (!description) return
    const open = description.classList.toggle('is-open')
    button.textContent = open ? 'Show less' : 'Show more'
    button.setAttribute('aria-expanded', open ? 'true' : 'false')
}

function cards(carousel: HTMLElement): HTMLElement[] {
    return Array.from(
        carousel.querySelectorAll<HTMLElement>(
            '.api-code-carousel-card[data-lang]'
        )
    )
}

function activeLanguage(carousel: HTMLElement): string | undefined {
    return carousel.querySelector<HTMLElement>(
        '.api-code-carousel-card.is-active'
    )?.dataset.lang
}

/** Marks a card active and updates the position label, arrows and dots. Returns false if the language is missing. */
function setActive(carousel: HTMLElement, language: string): boolean {
    const all = cards(carousel)
    const index = all.findIndex((card) =>
        sameLanguage(card.dataset.lang, language)
    )
    if (index < 0) return false

    all.forEach((card, i) => card.classList.toggle('is-active', i === index))
    syncStripHeight(carousel)
    carousel
        .querySelectorAll<HTMLElement>('[data-carousel-dot]')
        .forEach((dot) => {
            const match = sameLanguage(dot.dataset.carouselDot, language)
            dot.classList.toggle('is-active', match)
            dot.setAttribute('aria-current', match ? 'true' : 'false')
        })

    const position = carousel.querySelector<HTMLElement>(
        '[data-carousel-position]'
    )
    if (position) {
        const label = all.length === 1 ? 'language' : 'languages'
        const name = document.createElement('strong')
        name.textContent = all[index].dataset.lang ?? language
        const count = document.createElement('span')
        count.textContent = `${index + 1} / ${all.length} ${label}`
        position.replaceChildren(name, ' ', count)
    }

    carousel
        .querySelectorAll<HTMLButtonElement>('[data-carousel-step]')
        .forEach((button) => {
            const step = Number(button.dataset.carouselStep)
            button.disabled =
                (step < 0 && index === 0) ||
                (step > 0 && index === all.length - 1)
        })
    return true
}

const holds = new WeakMap<HTMLElement, () => void>()

/**
 * While the strip glides to a chosen language it passes the cards in between. They must not become active on the
 * way, or the strip resizes to each of them in turn. Released when the scroll ends, with a timeout as a fallback
 * for browsers without the scrollend event. Mandatory snapping is off while it glides: Firefox snaps back to
 * the previous card when one smooth scroll interrupts another. Consecutive picks share one hold, and when it
 * ends the strip is put on the active card if the scroll ended anywhere else.
 */
function holdActiveUntilScrolled(carousel: HTMLElement, strip: HTMLElement) {
    holds.get(carousel)?.()
    carousel.dataset.scrolling = 'true'
    strip.style.scrollSnapType = 'none'
    const cancel = () => {
        holds.delete(carousel)
        strip.removeEventListener('scrollend', release)
        window.clearTimeout(timer)
    }
    const release = () => {
        cancel()
        delete carousel.dataset.scrolling
        strip.style.removeProperty('scroll-snap-type')
        landOnActiveCard(carousel, strip)
    }
    const timer = window.setTimeout(release, 900)
    strip.addEventListener('scrollend', release, { once: true })
    holds.set(carousel, cancel)
}

/** Puts the strip on the active card without animation, when a scroll ended somewhere else. */
function landOnActiveCard(carousel: HTMLElement, strip: HTMLElement) {
    const active = cards(carousel).find((c) =>
        c.classList.contains('is-active')
    )
    if (!active) return
    const left = active.offsetLeft - strip.offsetLeft
    if (Math.abs(strip.scrollLeft - left) <= 1) return
    if (typeof strip.scrollTo === 'function')
        strip.scrollTo({ left, behavior: 'auto' })
    else strip.scrollLeft = left
}

/** Scrolls the strip to a language and marks it active. */
function showLanguage(
    carousel: HTMLElement,
    language: string,
    smooth = true
): boolean {
    const card = cards(carousel).find((c) =>
        sameLanguage(c.dataset.lang, language)
    )
    const strip = carousel.querySelector<HTMLElement>('[data-carousel-strip]')
    if (!card || !strip) return false

    setActive(carousel, card.dataset.lang ?? language)
    const left = card.offsetLeft - strip.offsetLeft
    const animate = smooth && !prefersReducedMotion()
    if (animate) holdActiveUntilScrolled(carousel, strip)
    if (typeof strip.scrollTo === 'function')
        strip.scrollTo({ left, behavior: animate ? 'smooth' : 'auto' })
    else strip.scrollLeft = left
    return true
}

function step(carousel: HTMLElement, delta: number): string | undefined {
    const all = cards(carousel)
    const current = all.findIndex((card) =>
        card.classList.contains('is-active')
    )
    const next = all[Math.max(0, Math.min(all.length - 1, current + delta))]
    if (!next?.dataset.lang) return undefined
    showLanguage(carousel, next.dataset.lang)
    return next.dataset.lang
}

function scenarioPanels(root: ParentNode): HTMLElement[] {
    return Array.from(
        root.querySelectorAll<HTMLElement>(
            '.api-examples-scenario-panel[data-scenario]'
        )
    )
}

function visiblePanel(examples: HTMLElement): HTMLElement | undefined {
    return scenarioPanels(examples).find((p) => !p.hasAttribute('hidden'))
}

function visibleCarousel(examples: HTMLElement): HTMLElement | null {
    return (visiblePanel(examples) ?? examples).querySelector<HTMLElement>(
        '[data-api-carousel]'
    )
}

/** Shows one scenario; the others stay findable through hidden="until-found". */
function showScenario(examples: HTMLElement, scenarioId: string): void {
    const panels = scenarioPanels(examples)
    if (!panels.some((p) => p.dataset.scenario === scenarioId)) return

    panels.forEach((panel) => {
        if (panel.dataset.scenario === scenarioId)
            panel.removeAttribute('hidden')
        else panel.setAttribute('hidden', 'until-found')
    })
    examples
        .querySelectorAll<HTMLElement>('.api-example-chip[data-scenario]')
        .forEach((chip) => {
            const match = chip.dataset.scenario === scenarioId
            chip.classList.toggle('is-active', match)
            chip.setAttribute('aria-selected', match ? 'true' : 'false')
        })
    const carousel = visibleCarousel(examples)
    if (carousel) syncStripHeight(carousel)
    syncDescription(examples)
}

function currentScenario(examples: HTMLElement): string | undefined {
    return visiblePanel(examples)?.dataset.scenario
}

/**
 * Applies the remembered language to the visible carousel, falling back to Console, then the first card.
 * The server marks the first card active, so this runs with transitions off: the reader should see the
 * remembered language settled, not the first card shrinking away from it on every page load.
 */
function applyPreferredLanguage(examples: HTMLElement, preferred?: string) {
    const carousel = visibleCarousel(examples)
    if (!carousel) return
    carousel.classList.add('is-settling')
    const picked = [preferred, savedApiLanguage(), defaultLanguage].some(
        (language) => !!language && showLanguage(carousel, language, false)
    )
    const first = cards(carousel)[0]?.dataset.lang
    if (!picked && first) showLanguage(carousel, first, false)
    // Flush the settled styles before transitions come back, or they animate the change after all.
    void carousel.offsetHeight
    carousel.classList.remove('is-settling')
}

function pickLanguage(examples: HTMLElement, language: string) {
    saveApiLanguage(language)
    writeDeepLink(language, currentScenario(examples))
}

/** Follows swipes and free scrolls: the card that settles into view becomes the pick, like a dot click would. */
function observeStrip(carousel: HTMLElement) {
    const strip = carousel.querySelector<HTMLElement>('[data-carousel-strip]')
    const examples = carousel.closest<HTMLElement>('[data-api-examples]')
    if (!strip || !examples || strip.dataset.carouselObserved) return
    strip.dataset.carouselObserved = 'true'
    if (typeof IntersectionObserver === 'undefined') return

    const observer = new IntersectionObserver(
        (entries) => {
            const best = entries
                .filter((entry) => entry.isIntersecting)
                .sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0]
            const language = (best?.target as HTMLElement | undefined)?.dataset
                .lang
            if (carousel.dataset.scrolling) return
            if (!best || best.intersectionRatio < 0.6 || !language) return
            // Only a change counts: the first callback after load reports the card that is already active.
            if (sameLanguage(language, activeLanguage(carousel))) return
            if (setActive(carousel, language)) pickLanguage(examples, language)
        },
        { root: strip, threshold: [0.6, 0.9] }
    )
    cards(carousel).forEach((card) => observer.observe(card))

    // Code is highlighted and numbered after load, which changes its height.
    if (typeof ResizeObserver === 'undefined') return
    const resize = new ResizeObserver(() => syncStripHeight(carousel))
    strip
        .querySelectorAll<HTMLElement>('.api-code-carousel-card-body > *')
        .forEach((content) => resize.observe(content))
}

function iconButton(className: string, label: string, icon: string) {
    const button = document.createElement('button')
    button.type = 'button'
    button.className = className
    button.dataset.tippyContent = label
    button.setAttribute('aria-label', label)
    button.innerHTML = icon
    return button
}

/** The code a preview of the card would show: its visible panel's, or its only block. Null when there is none. */
function previewCode(card: HTMLElement): HTMLElement | null {
    if (card.querySelector('[data-code-panel]'))
        return card.querySelector<HTMLElement>(
            '[data-code-panel]:not([hidden]) pre code'
        )
    return card.querySelector<HTMLElement>('pre code')
}

/**
 * Puts a full screen preview button in front of the copy button of every code card that has code.
 * For a card with status panels, CSS hides the button while the visible panel has no code (api-docs.css).
 */
function addPreviewButtons(examples: HTMLElement) {
    examples
        .querySelectorAll<HTMLElement>('[data-code-card]')
        .forEach((card) => {
            const actions = card.querySelector<HTMLElement>(
                '[data-code-actions]'
            )
            if (!actions || actions.querySelector('[data-code-preview]')) return
            if (!card.querySelector('pre code')) return
            const button = iconButton(
                'api-code-preview-btn',
                'Full screen preview',
                fullscreenIcon
            )
            button.dataset.codePreview = ''
            actions.prepend(button)
        })
}

/** The card's header, rebuilt for the dialog: badge, language and client label, or the response status. */
function previewHeader(card: HTMLElement, scenarioTitle: string): HTMLElement {
    const text = (selector: string) =>
        card.querySelector(selector)?.textContent?.trim() ?? ''
    const header = document.createElement('header')
    header.className = 'api-code-carousel-card-header'
    if (card.dataset.lang) {
        const badge = document.createElement('span')
        badge.className = 'api-lang-badge'
        badge.dataset.lang = card.dataset.lang
        badge.setAttribute('aria-hidden', 'true')
        header.appendChild(badge)
    }
    const title = document.createElement('span')
    title.className = 'api-code-carousel-card-title'
    header.appendChild(title)
    const name = document.createElement('span')
    name.className = 'api-code-carousel-card-language'
    const status = text('.example-response-tab.is-active')
    name.textContent =
        text('.api-code-carousel-card-language') ||
        (status ? `Response ${status}` : 'Code')
    title.appendChild(name)
    const client = card.querySelector<HTMLElement>(
        '.api-code-carousel-card-client'
    )
    if (client) title.appendChild(client.cloneNode(true))
    if (scenarioTitle) {
        const scenario = document.createElement('span')
        scenario.className = 'api-code-carousel-card-client'
        scenario.textContent = client ? `· ${scenarioTitle}` : scenarioTitle
        title.appendChild(scenario)
    }
    return header
}

/** The code of a card, almost full screen. Shows the visible panel of switchable cards. */
function openPreview(card: HTMLElement): HTMLDialogElement | null {
    const code = previewCode(card)
    if (!code) return null
    const block =
        code.closest<HTMLElement>('.notranslate') ?? code.closest('pre') ?? code
    const scenarioTitle =
        card
            .closest('[data-api-examples]')
            ?.querySelector(
                '.api-example-chip.is-active .api-example-chip-title'
            )
            ?.textContent?.trim() ?? ''

    // A code card in a dialog: same header, border and surface as the cards in the rail.
    const dialog = document.createElement('dialog')
    dialog.className = 'api-code-card api-code-preview'
    dialog.dataset.codeCard = ''
    dialog.setAttribute('aria-label', 'Full screen code preview')

    const header = previewHeader(card, scenarioTitle)
    const actions = document.createElement('span')
    actions.className = 'api-code-carousel-card-actions'
    actions.dataset.codeActions = ''
    const copy = iconButton('api-code-preview-btn', 'Copy code', iconCopyEui)
    copy.addEventListener('click', () => {
        // No Clipboard API (insecure context, old browser): nothing to copy with, so nothing to do.
        const write = navigator.clipboard?.writeText(
            code.textContent?.trimEnd() ?? ''
        )
        if (!write) return
        write.then(
            () => {
                temporarilyChangeIcon(copy, iconCopyEui, iconCheckEui)
                flashTooltip(copy, 'Copied!')
            },
            // Denied by permission or policy: the icon stays put, and the error is logged like the code block copy button does.
            (error: unknown) => console.error(error)
        )
    })
    const close = iconButton('api-code-preview-btn', 'Close', closeIcon)
    close.addEventListener('click', () => dialog.close())
    actions.append(copy, close)
    header.appendChild(actions)

    const body = document.createElement('div')
    body.className = 'api-code-preview-body'
    const clone = block.cloneNode(true) as HTMLElement
    clone.querySelectorAll('.copybtn').forEach((b) => b.remove())
    body.appendChild(clone)

    dialog.append(header, body)
    dialog.addEventListener('click', (event) => {
        if (event.target === dialog) dialog.close()
    })
    // Escape closes a modal dialog natively, unless another handler on the page prevents the keydown;
    // the site chrome does, so close explicitly and keep the key from reaching it.
    dialog.addEventListener('keydown', (event) => {
        if (event.key !== 'Escape') return
        event.preventDefault()
        event.stopPropagation()
        dialog.close()
    })
    const unlockScroll = lockPageScroll()
    dialog.addEventListener('close', () => {
        unlockScroll()
        dialog.remove()
    })

    document.body.appendChild(dialog)
    if (typeof dialog.showModal === 'function') dialog.showModal()
    else dialog.setAttribute('open', '')
    // showModal focuses the first control, the copy button, and a focused control shows its tooltip.
    // Focus starts on the dialog instead; the first Tab reaches the copy button.
    dialog.tabIndex = -1
    dialog.focus()
    return dialog
}

function onClick(event: MouseEvent) {
    const target = event.target as HTMLElement | null
    const examples = target?.closest<HTMLElement>('[data-api-examples]')
    if (!target || !examples) return

    const more = target.closest<HTMLElement>('[data-description-toggle]')
    if (more) {
        toggleDescription(more)
        return
    }

    if (target.closest('[data-code-preview]')) {
        const card = target.closest<HTMLElement>('[data-code-card]')
        if (card) openPreview(card)
        return
    }

    const chip = target.closest<HTMLElement>('.api-example-chip[data-scenario]')
    if (chip?.dataset.scenario) {
        const language = activeLanguage(visibleCarousel(examples) ?? examples)
        showScenario(examples, chip.dataset.scenario)
        applyPreferredLanguage(examples, language)
        writeDeepLink(
            activeLanguage(visibleCarousel(examples) ?? examples),
            chip.dataset.scenario
        )
        return
    }

    const carousel = target.closest<HTMLElement>('[data-api-carousel]')
    if (!carousel) return

    const dot = target.closest<HTMLElement>('[data-carousel-dot]')
    if (dot?.dataset.carouselDot) {
        showLanguage(carousel, dot.dataset.carouselDot)
        pickLanguage(examples, dot.dataset.carouselDot)
        return
    }

    const arrow = target.closest<HTMLElement>('[data-carousel-step]')
    if (arrow) {
        const language = step(carousel, Number(arrow.dataset.carouselStep))
        if (language) pickLanguage(examples, language)
        return
    }

    // The dimmed neighbour peeking in from the edge brings its language into view.
    const peek = target.closest<HTMLElement>(
        '.api-code-carousel-card:not(.is-active)'
    )
    if (peek?.dataset.lang && !window.getSelection()?.toString()) {
        showLanguage(carousel, peek.dataset.lang)
        pickLanguage(examples, peek.dataset.lang)
    }
}

function onKeydown(event: KeyboardEvent) {
    const target = event.target as HTMLElement | null
    if (!target || event.metaKey || event.ctrlKey || event.altKey) return

    const strip = target.closest<HTMLElement>('[data-carousel-strip]')
    if (strip && (event.key === 'ArrowRight' || event.key === 'ArrowLeft')) {
        const carousel = strip.closest<HTMLElement>('[data-api-carousel]')
        const examples = strip.closest<HTMLElement>('[data-api-examples]')
        if (!carousel || !examples) return
        event.preventDefault()
        const language = step(carousel, event.key === 'ArrowRight' ? 1 : -1)
        if (language) pickLanguage(examples, language)
        return
    }
}

/** Initializes every examples rail in `root`. Safe to call again after an HTMX swap. */
export function initApiExamples(root: ParentNode = document): void {
    if (!delegated) {
        delegated = true
        document.addEventListener('click', onClick)
        document.addEventListener('keydown', onKeydown)
    }
    initTooltips()

    const link = readDeepLink()
    root.querySelectorAll<HTMLElement>('[data-api-examples]').forEach(
        (examples) => {
            addPreviewButtons(examples)
            if (link.example) showScenario(examples, link.example)
            scenarioPanels(examples).forEach((panel) => {
                if (panel.dataset.beforematchBound) return
                panel.dataset.beforematchBound = 'true'
                panel.addEventListener('beforematch', () => {
                    if (panel.dataset.scenario)
                        showScenario(examples, panel.dataset.scenario)
                })
            })
            examples
                .querySelectorAll<HTMLElement>('[data-api-carousel]')
                .forEach(observeStrip)
            applyPreferredLanguage(examples, link.lang)
            if (!link.example) syncDescription(examples)
        }
    )
}
