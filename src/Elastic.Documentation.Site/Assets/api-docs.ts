/**
 * API Documentation interactive features
 * Handles expand/collapse toggles, scroll state, and find-in-page support
 * for both OperationView and SchemaView pages.
 */
import { decorateApiCodeTokens } from './api-code-tokens'
import { initApiExamples } from './api-examples-carousel'
import { fitParamSummaries } from './api-param-summary'
import { initUnionChips, revealVariantsAround } from './api-union-chips'
import { iconCheckEui, iconCopyEui } from './copybutton'
import { prefersReducedMotion } from './motion'
import { flashTooltip, initTooltips } from './tooltip'

// Check if hidden="until-found" is supported (for find-in-page in collapsed sections)
const supportsHiddenUntilFound = 'onbeforematch' in document.body

function setDisclosureToggle(
    toggleBtn: Element | null,
    expanded: boolean,
    noun?: string
): void {
    if (!toggleBtn) return
    toggleBtn.setAttribute('aria-expanded', expanded ? 'true' : 'false')
    const icon = toggleBtn.querySelector('.toggle-icon')
    const label = toggleBtn.querySelector('.toggle-label')
    if (icon) icon.textContent = expanded ? '−' : '+'
    if (label && noun)
        label.textContent = `${expanded ? 'hide' : 'show'} ${noun}`
}

function setUntilFoundHidden(
    element: HTMLElement | null,
    hidden: boolean
): void {
    if (!element) return
    if (!hidden) {
        element.removeAttribute('hidden')
        return
    }
    element.setAttribute(
        'hidden',
        supportsHiddenUntilFound ? 'until-found' : ''
    )
}

function setEnumValuesExpanded(
    toggleBtn: HTMLButtonElement,
    expanded: boolean
): void {
    const folded = toggleBtn.parentElement?.querySelector<HTMLElement>(
        ':scope > .enum-values-folded'
    )
    setUntilFoundHidden(folded ?? null, !expanded)
    setDisclosureToggle(
        toggleBtn,
        expanded,
        `${folded?.childElementCount ?? 0} more values`
    )
}

interface Disclosure {
    container: string
    toggle: string
    body: string
    trigger: string
    noun?: string
}

const RESPONSE_DISCLOSURE: Disclosure = {
    container: '.response-panel',
    toggle: ':scope > .response-summary .response-status-toggle',
    body: ':scope > .response-panel-body',
    trigger: '.response-summary',
}

// Order matters where a row can match more than one entry: variants before properties.
const DISCLOSURES: Disclosure[] = [
    {
        container: '.union-variant-item',
        toggle: ':scope > .union-expand-toggle > .expand-toggle',
        body: ':scope > .nested-properties',
        trigger: '.union-expand-toggle > .expand-toggle',
        noun: 'properties',
    },
    {
        container: '.property-item',
        toggle: ':scope > .expand-toggle-row > .expand-toggle',
        body: ':scope > .nested-properties',
        trigger: '.expand-toggle-row > .expand-toggle',
        noun: 'properties',
    },
    {
        container: '[data-param-section]',
        toggle: ':scope > .api-param-section-header > .api-param-section-toggle',
        body: ':scope > .api-param-section-body',
        trigger:
            '.api-param-section-header > .headerlink, .api-param-section-toggle',
    },
    RESPONSE_DISCLOSURE,
]

function disclosureFor(el: Element): Disclosure | undefined {
    return DISCLOSURES.find((d) => el.matches(d.container))
}

function setDisclosureExpanded(
    container: HTMLElement,
    d: Disclosure,
    expanded: boolean
): void {
    container.classList.toggle('expanded', expanded)
    container.classList.toggle('collapsed', !expanded)
    setDisclosureToggle(container.querySelector(d.toggle), expanded, d.noun)
    setUntilFoundHidden(container.querySelector<HTMLElement>(d.body), !expanded)
    if (expanded && d === RESPONSE_DISCLOSURE) onResponseOpened(container)
}

/** Responses behave as an accordion and keep the examples rail on the same status code. */
function onResponseOpened(panel: HTMLElement): void {
    panel
        .closest('[data-api-responses]')
        ?.querySelectorAll<HTMLElement>('.response-panel.expanded')
        .forEach((other) => {
            if (other !== panel)
                setDisclosureExpanded(other, RESPONSE_DISCLOSURE, false)
        })
    const status = panel.dataset.status
    if (!status) return
    document
        .querySelectorAll<HTMLElement>('[data-api-response-samples]')
        .forEach((widget) => {
            // applyApiResponseStatus falls back to the first tab, so skip rails without this status
            if (
                widget.querySelector(
                    `.example-response-tab[data-status="${status}"]`
                )
            )
                applyApiResponseStatus(widget, status)
        })
}

function openMainResponse(status: string): void {
    const panel = document.querySelector<HTMLElement>(
        `[data-api-responses] .response-panel:not(.response-panel--static)[data-status="${status}"]`
    )
    if (panel) setDisclosureExpanded(panel, RESPONSE_DISCLOSURE, true)
}

/**
 * Opens every collapsed disclosure whose body holds a link or find-in-page target, and switches
 * every variant chip list on the way to the variant that holds it, so the target lands on visible
 * content in every browser, including those without hidden="until-found". A row that only holds
 * the target in its own header stays as it was.
 */
export function revealCollapsedAncestors(target: HTMLElement): boolean {
    let revealed = revealVariantsAround(target)
    for (let el = target.parentElement; el; el = el.parentElement) {
        if (!el.classList.contains('collapsed')) continue
        const d = disclosureFor(el)
        if (!d || !el.querySelector(d.body)?.contains(target)) continue
        setDisclosureExpanded(el, d, true)
        revealed = true
    }
    return revealed
}

let paramSummaryObserver: ResizeObserver | null = null
let paramHashListenerBound = false

function fitAllParamSummaries(): void {
    fitParamSummaries(
        document.querySelectorAll<HTMLElement>('[data-param-summary]')
    )
}

function initParamSummaries(): void {
    paramSummaryObserver?.disconnect()
    // The observer reports every summary once on observe(), which does the first fit
    paramSummaryObserver = new ResizeObserver((entries) =>
        fitParamSummaries(entries.map((entry) => entry.target as HTMLElement))
    )

    document
        .querySelectorAll<HTMLElement>('[data-param-summary]')
        .forEach((summary) => paramSummaryObserver?.observe(summary))

    // A web font landing changes the name widths but not the summary's size
    if (document.fonts && document.fonts.status !== 'loaded')
        void document.fonts.ready.then(fitAllParamSummaries)

    if (!paramHashListenerBound) {
        paramHashListenerBound = true
        window.addEventListener('hashchange', revealHashTarget)
    }
}

/**
 * Opens what hides a link target the way find-in-page does: each hidden="until-found" ancestor
 * gets a beforematch event, so a widget such as the authentication tabs can select the panel
 * that holds the target before it is shown.
 */
export function revealHiddenAncestors(target: HTMLElement): boolean {
    let revealed = false
    for (let el = target.parentElement; el; el = el.parentElement) {
        if (el.getAttribute('hidden') !== 'until-found') continue
        el.dispatchEvent(new Event('beforematch', { bubbles: true }))
        setUntilFoundHidden(el, false)
        revealed = true
    }
    return revealed
}

function revealHashTarget(): void {
    const id = window.location.hash.slice(1)
    const target = id ? document.getElementById(id) : null
    if (!target) return
    // Not short-circuited: a section can already be open while a tab inside it is not
    const collapsed = revealCollapsedAncestors(target)
    const hidden = revealHiddenAncestors(target)
    if (collapsed || hidden) target.scrollIntoView()
}

/**
 * Initialize API docs for OperationView pages
 */
let examplesScrollBound = false

/** Shows the "jump to examples" button while the examples are still below the fold. */
function updateExamplesButtonVisibility(): void {
    const examplesBtn = document.getElementById('examples-jump-btn')
    const examplesSection = document.querySelector(
        '#elastic-api-v3 h3[data-section="request-examples"], #elastic-api-v3 h3[data-section="response-examples"]'
    )
    if (!examplesBtn || !examplesSection) return
    const belowFold =
        examplesSection.getBoundingClientRect().top > window.innerHeight
    examplesBtn.classList.toggle('visible', belowFold)
}

function initOperationView(): void {
    // Bound once: htmx swaps the section, so the handler looks the elements up each time
    if (!examplesScrollBound) {
        examplesScrollBound = true
        let ticking = false
        window.addEventListener('scroll', () => {
            if (ticking) return
            ticking = true
            window.requestAnimationFrame(() => {
                updateExamplesButtonVisibility()
                ticking = false
            })
        })
    }
    updateExamplesButtonVisibility()
}

// Track if global handlers have been initialized
let globalHandlersInitialized = false

/**
 * Initialize global click handlers for expand/collapse functionality
 * Uses event delegation at document level so it works after HTMX content swaps
 */
function initGlobalClickHandlers(): void {
    if (globalHandlersInitialized) return
    globalHandlersInitialized = true

    // Find-in-page matched hidden content: unfold the enum row or open the disclosures around it
    document.addEventListener('beforematch', function (e) {
        const target = e.target as HTMLElement
        const folded = target.closest('.enum-values-folded')
        const toggleBtn =
            folded?.parentElement?.querySelector<HTMLButtonElement>(
                ':scope > .enum-values-toggle'
            )
        if (toggleBtn) setEnumValuesExpanded(toggleBtn, true)
        else revealCollapsedAncestors(target)
    })

    document.addEventListener('click', function (e) {
        const target = e.target as HTMLElement

        // Only handle clicks within API doc sections
        const apiSection = target.closest(
            '#elastic-api-v3, #schema-definition'
        ) as HTMLElement
        if (!apiSection) return

        // Before the generic .expand-toggle branch, which would toggle the enclosing property instead
        const enumValuesToggle = target.closest<HTMLButtonElement>(
            '.enum-values-toggle'
        )
        if (enumValuesToggle) {
            e.preventDefault()
            e.stopPropagation()
            setEnumValuesExpanded(
                enumValuesToggle,
                enumValuesToggle.getAttribute('aria-expanded') !== 'true'
            )
            return
        }

        for (const d of DISCLOSURES) {
            const trigger = target.closest<HTMLElement>(d.trigger)
            const container = trigger?.closest<HTMLElement>(d.container)
            if (!trigger || !container) continue
            if (!shouldToggleOnClick(e, trigger)) return

            e.preventDefault()
            e.stopPropagation()
            setDisclosureExpanded(
                container,
                d,
                !container.classList.contains('expanded')
            )
            // pushState sets the hash without a hashchange, so the page doesn't jump to the
            // title and the section the click just collapsed isn't re-opened.
            if (
                trigger instanceof HTMLAnchorElement &&
                window.location.hash !== trigger.hash
            )
                history.pushState(null, '', trigger.hash)
            return
        }

        const link = target.closest<HTMLAnchorElement>('a[href^="#"]')
        if (link) scrollToLinkTarget(e, link)
    })
}

/**
 * Opens the rows around an in-page link's target, then scrolls to it smoothly. The browser's own
 * jump would skip the smooth scroll, and pushState fires no hashchange to open the rows.
 */
function scrollToLinkTarget(e: MouseEvent, link: HTMLAnchorElement): void {
    if (e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey)
        return
    const href = link.getAttribute('href')!
    const target = document.getElementById(href.slice(1))
    if (!target) return

    e.preventDefault()
    revealCollapsedAncestors(target)
    revealHiddenAncestors(target)
    target.scrollIntoView({ behavior: 'smooth' })
    if (window.location.hash !== href) history.pushState(null, '', href)
}

/**
 * Links inside a trigger navigate, modified clicks on a trigger link keep normal link
 * behaviour, and finishing a text selection inside a trigger must not toggle.
 */
function shouldToggleOnClick(e: MouseEvent, trigger: HTMLElement): boolean {
    const link = (e.target as HTMLElement).closest('a')
    if (link && link !== trigger && trigger.contains(link)) return false
    if (trigger instanceof HTMLAnchorElement)
        if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return false
    const selection = window.getSelection()
    return !(
        selection &&
        !selection.isCollapsed &&
        (trigger.contains(selection.anchorNode) ||
            trigger.contains(selection.focusNode))
    )
}

/** Show only the header copy button that belongs to the active panel. */
function syncCopyButtons(widget: HTMLElement, activePanel: string): void {
    widget
        .querySelectorAll<HTMLButtonElement>('[data-code-actions] .copybtn')
        .forEach((button) => {
            button.hidden = button.dataset.codePanel !== activePanel
        })
}

function applyApiResponseStatus(widget: HTMLElement, statusCode: string): void {
    const tabs = Array.from(
        widget.querySelectorAll<HTMLElement>(
            '.example-response-tab[data-status]'
        )
    )
    const hasStatus = tabs.some((tab) => tab.dataset.status === statusCode)
    const effective = hasStatus
        ? statusCode
        : (tabs[0]?.dataset.status ?? statusCode)

    tabs.forEach((tab) => {
        const match = tab.dataset.status === effective
        tab.classList.toggle('is-active', match)
        tab.setAttribute('aria-selected', match ? 'true' : 'false')
        tab.tabIndex = match ? 0 : -1
    })

    widget
        .querySelectorAll<HTMLElement>('.example-response-panel')
        .forEach((panel) => {
            panel.toggleAttribute('hidden', panel.dataset.status !== effective)
        })

    syncCopyButtons(widget, effective)
}

let apiResponseStatusTabsDelegated = false

/** Status-code tabs on response example cards in the examples rail. */
function initApiResponseStatusTabs(): void {
    if (apiResponseStatusTabsDelegated) return
    apiResponseStatusTabsDelegated = true
    document.addEventListener('click', (event) => {
        const tab = (event.target as HTMLElement | null)?.closest(
            '.example-response-tab[data-status]'
        )
        if (!(tab instanceof HTMLElement) || !tab.dataset.status) return
        const widget = tab.closest<HTMLElement>('[data-api-response-samples]')
        if (!widget) return
        applyApiResponseStatus(widget, tab.dataset.status)
        openMainResponse(tab.dataset.status)
    })
}

let apiEndpointCopyInitialized = false
let apiPageActionsInitialized = false
let apiPropertyAnchorCopyInitialized = false

function closestEndpointCopyButton(
    target: EventTarget | null
): HTMLButtonElement | null {
    if (!(target instanceof Element)) return null
    return target.closest<HTMLButtonElement>('button.api-url-copy')
}

function initApiEndpointCopy(): void {
    if (apiEndpointCopyInitialized) return
    apiEndpointCopyInitialized = true

    document.addEventListener(
        'mousedown',
        (e) => {
            if (
                !(e.target instanceof Element) ||
                !e.target.closest('button.api-url-copy')
            )
                return
            e.preventDefault()
            e.stopPropagation()
        },
        true
    )

    document.addEventListener(
        'click',
        (e) => {
            const btn = closestEndpointCopyButton(e.target)
            if (!btn) return

            e.preventDefault()
            e.stopPropagation()

            const text = btn.dataset.copy ?? ''
            if (!text) return

            void navigator.clipboard.writeText(text).then(
                () => {
                    btn.classList.add('success')
                    flashTooltip(btn, 'Copied!')
                    btn.innerHTML = iconCheckEui
                    window.setTimeout(() => {
                        btn.classList.remove('success')
                        btn.innerHTML = iconCopyEui
                    }, 1500)
                },
                (error) => {
                    console.error(error)
                }
            )
        },
        true
    )
}

function initApiPropertyAnchorCopy(): void {
    if (apiPropertyAnchorCopyInitialized) return
    apiPropertyAnchorCopyInitialized = true

    // The link icon still moves the page to the property (the click is not
    // cancelled); it also copies the full URL so the link can be shared.
    document.addEventListener('click', (e) => {
        if (e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey)
            return
        if (!(e.target instanceof Element)) return
        const icon = e.target.closest<HTMLElement>('.property-anchor-icon')
        const href = icon
            ?.closest<HTMLAnchorElement>('a.property-anchor')
            ?.getAttribute('href')
        if (!icon || !href) return

        const url = new URL(href, window.location.href).href
        void navigator.clipboard?.writeText(url).then(
            () => flashTooltip(icon, 'Copied!'),
            (error) => console.error(error)
        )
    })
}

function closestCopyPageTrigger(
    target: EventTarget | null
): HTMLElement | null {
    if (!(target instanceof Element)) return null
    const trigger = target.closest<HTMLElement>('[data-copy-page]')
    return trigger?.dataset.copyPage ? trigger : null
}

async function copyPageMarkdown(
    url: string,
    root: Element | null
): Promise<void> {
    const response = await fetch(url)
    if (!response.ok) throw new Error(`Copy page failed: ${response.status}`)
    const text = await response.text()
    await navigator.clipboard.writeText(text)
    const label = root?.querySelector<HTMLElement>('.api-page-actions-label')
    if (!label) return
    const original = label.textContent
    label.textContent = 'Copied!'
    window.setTimeout(() => {
        if (original) label.textContent = original
    }, 1500)
}

export function initApiPageActions(): void {
    if (apiPageActionsInitialized) return
    apiPageActionsInitialized = true

    document.addEventListener('click', (event) => {
        const trigger = closestCopyPageTrigger(event.target)
        if (!trigger || !trigger.dataset.copyPage) return

        event.preventDefault()
        const root = trigger.closest('.api-page-actions')
        const dropdown = root?.querySelector<HTMLDetailsElement>(
            '.api-page-actions-dropdown'
        )
        if (dropdown) dropdown.open = false
        void copyPageMarkdown(trigger.dataset.copyPage, root).catch((error) => {
            console.error(error)
        })
    })
}

let examplesRailSteadyInitialized = false
let examplesRailSlide: Animation | undefined

/**
 * Expanding or collapsing a section flips the sticky examples rail between stuck and
 * bottom-pinned, which moved it hundreds of px in one frame. Slide it there instead.
 */
export function initExamplesRailSteadying(): void {
    if (examplesRailSteadyInitialized) return
    examplesRailSteadyInitialized = true
    // Capture: some toggle handlers stopPropagation, and we must measure before they mutate
    document.addEventListener(
        'click',
        (e) => {
            if (!(e.target instanceof Element)) return
            if (!e.target.closest('#elastic-api-v3')) return
            // The rail is only sticky from 1024px up; below that it moves with the page
            if (!window.matchMedia?.('(min-width: 1024px)').matches) return
            if (prefersReducedMotion()) return
            const rail = document.getElementById('api-examples-panel')
            if (!rail) return
            // The in-flight slide's transform shows up in the rect. Keep that visual
            // position, cancel the slide, then measure the untransformed layout.
            const visualTop = rail.getBoundingClientRect().top
            // Cancel only our own slide: getAnimations() would also cancel the rail's CSS transitions.
            examplesRailSlide?.cancel()
            const before = rail.getBoundingClientRect().top
            const inFlight = visualTop - before
            // Runs after the click handlers change the DOM, before the next paint
            requestAnimationFrame(() => {
                const offset =
                    inFlight - (rail.getBoundingClientRect().top - before)
                if (Math.abs(offset) < 1) return
                examplesRailSlide = rail.animate(
                    [
                        { transform: `translateY(${offset}px)` },
                        { transform: 'none' },
                    ],
                    { duration: 200, easing: 'ease-out' }
                )
            })
        },
        true
    )
}

/**
 * Initialize API documentation interactivity
 * Call this after page load or HTMX content swap
 */
export function initApiDocs(): void {
    // Initialize global click handlers once (uses event delegation)
    initGlobalClickHandlers()
    initTooltips()
    initApiEndpointCopy()
    initApiPropertyAnchorCopy()
    initApiResponseStatusTabs()
    initApiExamples()
    initUnionChips()
    initApiPageActions()
    initExamplesRailSteadying()
    // After initHighlight — gutters need final textContent line counts
    decorateApiCodeTokens()

    // Check for OperationView page - initialize view-specific features
    initParamSummaries()

    const operationSection = document.getElementById('elastic-api-v3')
    if (operationSection) initOperationView()
    // Schema pages too: a link to a field inside a variant that is not picked must show that variant.
    revealHashTarget()
}
