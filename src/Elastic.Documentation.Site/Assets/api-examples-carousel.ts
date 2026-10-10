/**
 * Examples rail on API operation pages: example chips pick a scenario, each scenario shows a
 * scroll-snap carousel with one card per language. Console is the default language; an explicit pick is remembered across pages.
 */
import { iconCheckEui, iconCopyEui, temporarilyChangeIcon } from './copybutton'
import { closeIcon, fullscreenIcon } from './icons'
import { prefersReducedMotion } from './motion'
import { lockPageScroll } from './scroll-lock'
import { flashTooltip, initTooltips } from './tooltip'
import Fuse from 'fuse.js'
import type { Instance } from 'tippy.js'

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
    fitChips(examples)
}

function currentScenario(examples: HTMLElement): string | undefined {
    return visiblePanel(examples)?.dataset.scenario
}

const maxChipRows = 2

/** Lines a run of items wraps into, the way flex-wrap lays them out. */
function lineCount(widths: number[], rowWidth: number, gap: number): number {
    let lines = 1
    let x = 0
    for (const width of widths.map((w) => Math.min(w, rowWidth))) {
        if (x > 0 && x + gap + width > rowWidth) {
            lines++
            x = width
        } else x = x > 0 ? x + gap + width : width
    }
    return lines
}

/**
 * Which chips stay in the row: all of them when they fit in `maxRows` lines, otherwise as many from the start as
 * fit next to the "more" button. The active chip always stays, taking the place of the last one that fits.
 */
export function packChips(
    widths: number[],
    rowWidth: number,
    gap: number,
    moreWidth: number,
    maxRows: number,
    activeIndex: number
): number[] {
    const all = widths.map((_, i) => i)
    if (lineCount(widths, rowWidth, gap) <= maxRows) return all

    const fits = (indexes: number[]) =>
        lineCount(
            [...indexes.map((i) => widths[i]), moreWidth],
            rowWidth,
            gap
        ) <= maxRows
    for (let keep = widths.length - 1; keep > 0; keep--) {
        const candidate = all.slice(0, keep)
        if (activeIndex >= keep) candidate[keep - 1] = activeIndex
        if (fits(candidate)) return candidate
    }
    return activeIndex >= 0 ? [activeIndex] : []
}

function chipMenuSupported(menu: HTMLElement | null): menu is HTMLElement {
    return !!menu && typeof menu.hidePopover === 'function'
}

/** Removes the tooltip text and the tippy instance already made from it, which keeps the old text. */
function clearChipTooltip(chip: HTMLElement) {
    delete chip.dataset.tippyContent
    ;(chip as HTMLElement & { _tippy?: Instance })._tippy?.destroy()
}

function setMoreLabel(more: HTMLElement, count: number) {
    more.querySelector('[data-example-more-label]')!.textContent =
        `+${count} more`
}

/** Moves the chips that do not fit in two lines into the "more" menu. Without popover support every chip stays. */
function fitChips(examples: HTMLElement) {
    const row = examples.querySelector<HTMLElement>('[data-example-chips]')
    // Until the first fit the row is clipped to two lines (api-docs.css), so the page does not jump when chips move into the menu.
    row?.classList.add('is-fitted')
    const more = row?.querySelector<HTMLElement>('[data-example-more]')
    const menu = row?.querySelector<HTMLElement>('[data-example-menu]') ?? null
    if (!row || !more || !chipMenuSupported(menu)) return

    const chips = Array.from(
        row.querySelectorAll<HTMLElement>('.api-example-chip[data-scenario]')
    )
    chips.forEach((chip) => (chip.hidden = false))
    more.hidden = false
    // Measured with the longest label it can get, so the room kept for it is enough.
    setMoreLabel(more, chips.length)
    const rowWidth = row.clientWidth
    // Unmeasurable (display: none, or no layout): leave every chip in place.
    if (rowWidth === 0) {
        more.hidden = true
        return
    }

    // Everything is measured before anything is written, so the fit costs one layout pass.
    const gap = parseFloat(getComputedStyle(row).columnGap) || 6
    // +1 because offsetWidth rounds down, and a chip a fraction too wide would wrap onto a third line.
    const widths = chips.map((chip) => chip.offsetWidth + 1)
    const titles = chips.map((chip) =>
        chip.querySelector<HTMLElement>('.api-example-chip-title')
    )
    const truncated = titles.map((t) => !!t && t.scrollWidth > t.clientWidth)
    const visible = new Set(
        packChips(
            widths,
            rowWidth,
            gap,
            more.offsetWidth + 1,
            maxChipRows,
            chips.findIndex((chip) => chip.classList.contains('is-active'))
        )
    )

    const hiddenCount = chips.length - visible.size
    const overflow = new Set<string | undefined>()
    chips.forEach((chip, i) => {
        chip.hidden = !visible.has(i)
        if (chip.hidden) overflow.add(chip.dataset.scenario)
        // A title cut off by the chip's width gets a tooltip with the whole of it; one that fits again loses it.
        else if (truncated[i])
            chip.dataset.tippyContent = titles[i]?.textContent?.trim() ?? ''
        else clearChipTooltip(chip)
    })
    menu.querySelectorAll<HTMLElement>('[data-scenario]').forEach((item) => {
        item.hidden = !overflow.has(item.dataset.scenario)
    })
    more.hidden = hiddenCount === 0
    setMoreLabel(more, hiddenCount)
    if (hiddenCount === 0) menu.hidePopover()
}

/** Refits when the rail changes width, since chip titles wrap differently. */
function observeChips(examples: HTMLElement) {
    const row = examples.querySelector<HTMLElement>('[data-example-chips]')
    if (
        !row ||
        row.dataset.chipsObserved ||
        typeof ResizeObserver === 'undefined'
    )
        return
    row.dataset.chipsObserved = 'true'
    let width = row.clientWidth
    new ResizeObserver(() => {
        // Only a change of width: refitting changes the row's height, which must not trigger another round.
        if (row.clientWidth === width) return
        width = row.clientWidth
        fitChips(examples)
    }).observe(row)
}

let openMenu: { menu: HTMLElement; more: HTMLElement } | null = null
let followQueued = false

/** Keeps the open menu with its button as the page scrolls or resizes, at most once a frame. */
function followOpenMenu() {
    if (!openMenu || followQueued) return
    followQueued = true
    requestAnimationFrame(() => {
        followQueued = false
        if (openMenu) positionMenu(openMenu.menu, openMenu.more)
    })
}

/** Under the "more" button, or above it when there is no room below. Run again on scroll, so the menu stays with its button. */
function positionMenu(menu: HTMLElement, more: HTMLElement) {
    const anchor = more.getBoundingClientRect()
    const size = menu.getBoundingClientRect()
    const margin = 8
    const left = Math.max(
        margin,
        Math.min(anchor.left, window.innerWidth - size.width - margin)
    )
    const below = anchor.bottom + 4
    const top =
        below + size.height > window.innerHeight - margin &&
        anchor.top - 4 - size.height > margin
            ? anchor.top - 4 - size.height
            : below
    menu.style.left = `${left}px`
    menu.style.top = `${top}px`
}

/** Opens under the "more" button, or above it when there is no room below, and keeps the button's state in step. */
function onMenuToggle(event: Event) {
    const menu = event.target as HTMLElement | null
    if (!menu?.matches('[data-example-menu]')) return
    const more = menu
        .closest<HTMLElement>('[data-example-chips]')
        ?.querySelector<HTMLElement>('[data-example-more]')
    const open = (event as ToggleEvent).newState === 'open'
    more?.setAttribute('aria-expanded', open ? 'true' : 'false')
    const filter = menu.querySelector<HTMLInputElement>('[data-example-filter]')
    openMenu = open && more ? { menu, more } : null
    if (!open && filter?.value) {
        filter.value = ''
        filterMenu(filter)
    }
    if (!open || !more) return

    positionMenu(menu, more)
    if (filter) filter.focus()
    else menuItems(menu)[0]?.focus()
}

function menuItems(menu: HTMLElement): HTMLElement[] {
    return Array.from(
        menu.querySelectorAll<HTMLElement>('[role="menuitem"]:not([hidden])')
    )
}

interface MenuSearch {
    fuse: Fuse<{ item: HTMLElement; title: string }>
    all: HTMLElement[]
}

const menuSearches = new WeakMap<HTMLElement, MenuSearch>()

function menuSearch(menu: HTMLElement): MenuSearch {
    let search = menuSearches.get(menu)
    if (!search) {
        const all = Array.from(
            menu.querySelectorAll<HTMLElement>('[role="menuitem"]')
        )
        const entries = all.map((item) => ({
            item,
            title:
                item
                    .querySelector('.api-example-menu-item-title')
                    ?.textContent?.trim() ??
                item.textContent?.trim() ??
                '',
        }))
        // Titles are long sentences: a match anywhere in them counts, and a typo or two is forgiven.
        search = {
            fuse: new Fuse(entries, {
                keys: ['title'],
                ignoreLocation: true,
                threshold: 0.35,
            }),
            all,
        }
        menuSearches.set(menu, search)
    }
    return search
}

/** Every word must match (fuzzily) somewhere in the title; the best combined score ranks first. */
function searchWords(fuse: MenuSearch['fuse'], query: string): HTMLElement[] {
    const scores = new Map<HTMLElement, number>()
    query
        .split(/\s+/)
        .filter(Boolean)
        .forEach((word, i) => {
            const found = new Map(
                fuse.search(word).map((r) => [r.item.item, r.score ?? 0])
            )
            if (i === 0) found.forEach((score, item) => scores.set(item, score))
            else
                scores.forEach((score, item) => {
                    const next = found.get(item)
                    if (next === undefined) scores.delete(item)
                    else scores.set(item, score + next)
                })
        })
    return [...scores.entries()]
        .sort((x, y) => x[1] - y[1])
        .map(([item]) => item)
}

/** Fuzzy filter: shows the examples that are not in the row and match, best match first. */
function filterMenu(input: HTMLInputElement) {
    const menu = input.closest<HTMLElement>('[data-example-menu]')
    const examples = input.closest<HTMLElement>('[data-api-examples]')
    if (!menu || !examples) return
    const inRow = new Set(
        Array.from(examples.querySelectorAll<HTMLElement>('.api-example-chip'))
            .filter((chip) => !chip.hidden)
            .map((chip) => chip.dataset.scenario)
    )
    const { fuse, all } = menuSearch(menu)
    const query = input.value.trim()
    const matches = query ? searchWords(fuse, query) : all
    const shown = matches.filter((item) => !inRow.has(item.dataset.scenario))
    const shownSet = new Set(shown)
    const parent = shown[0]?.parentElement ?? all[0]?.parentElement
    // Matches first, in rank order; the rest follow hidden. Without a query, the original order comes back.
    const order = [...shown, ...all.filter((item) => !shownSet.has(item))]
    order.forEach((item) => {
        item.hidden = !shownSet.has(item)
        parent?.appendChild(item)
    })
}

function selectScenario(examples: HTMLElement, scenarioId: string) {
    showScenario(examples, scenarioId)
    applyPreferredLanguage(examples)
    writeDeepLink(
        activeLanguage(visibleCarousel(examples) ?? examples),
        scenarioId
    )
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
    lastSettled.set(carousel, Date.now())
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

const settleWindowMs = 1000
const lastSettled = new WeakMap<HTMLElement, number>()

/**
 * Right after the page applies a language itself, the strip reports cards on the way: those are not picks.
 * Not the data-scrolling hold: that one only covers smooth glides, and this apply jumps without animation.
 */
function recentlySettled(carousel: HTMLElement): boolean {
    return (
        Date.now() - (lastSettled.get(carousel) ?? -Infinity) < settleWindowMs
    )
}

/**
 * Follows swipes, free scrolls and find-in-page: the card that settles into view becomes the pick, like a dot
 * click would. Not while the page is applying the remembered language itself: a fallback card settling into view
 * on its own must not replace the saved language.
 */
function observeStrip(carousel: HTMLElement) {
    const strip = carousel.querySelector<HTMLElement>('[data-carousel-strip]')
    const examples = carousel.closest<HTMLElement>('[data-api-examples]')
    if (!strip || !examples || strip.dataset.carouselObserved) return
    strip.dataset.carouselObserved = 'true'
    if (typeof IntersectionObserver === 'undefined') return

    const observer = new IntersectionObserver(
        (entries) => {
            if (carousel.dataset.scrolling || recentlySettled(carousel)) return
            const best = entries
                .filter((entry) => entry.isIntersecting)
                .sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0]
            const language = (best?.target as HTMLElement | undefined)?.dataset
                .lang
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

    const chip = target.closest<HTMLElement>(
        '.api-example-chip[data-scenario], .api-example-menu-item[data-scenario]'
    )
    if (chip?.dataset.scenario) {
        selectScenario(examples, chip.dataset.scenario)
        const menu = chip.closest<HTMLElement>('[data-example-menu]')
        if (menu) {
            menu.hidePopover()
            examples
                .querySelector<HTMLElement>('.api-example-chip.is-active')
                ?.focus()
        }
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

/** Arrow keys, Home and End move focus through the visible menu items. Returns whether the key was handled. */
function moveInMenu(menu: HTMLElement, from: HTMLElement, key: string) {
    const items = menuItems(menu)
    const index = items.indexOf(from)
    const next: Record<string, number> = {
        ArrowDown: Math.min(items.length - 1, index + 1),
        ArrowUp: index <= 0 ? 0 : index - 1,
        Home: 0,
        End: items.length - 1,
    }
    if (!(key in next) || items.length === 0) return false
    // In the filter field, Home and End move the caret.
    if (index < 0 && (key === 'Home' || key === 'End')) return false
    items[next[key]].focus()
    return true
}

function onKeydown(event: KeyboardEvent) {
    const target = event.target as HTMLElement | null
    if (!target || event.metaKey || event.ctrlKey || event.altKey) return

    const menu = target.closest<HTMLElement>('[data-example-menu]')
    if (menu && moveInMenu(menu, target, event.key)) {
        event.preventDefault()
        return
    }

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
        // toggle does not bubble, so it is caught on the way down.
        document.addEventListener('toggle', onMenuToggle, true)
        window.addEventListener('scroll', followOpenMenu, {
            capture: true,
            passive: true,
        })
        window.addEventListener('resize', followOpenMenu)
        document.addEventListener('input', (event) => {
            const input = event.target as HTMLElement | null
            if (input?.matches('[data-example-filter]'))
                filterMenu(input as HTMLInputElement)
        })
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
            fitChips(examples)
            observeChips(examples)
            // Web fonts change the chips' widths once they load.
            if (document.fonts && document.fonts.status !== 'loaded')
                void document.fonts.ready.then(() => fitChips(examples))
        }
    )
}
