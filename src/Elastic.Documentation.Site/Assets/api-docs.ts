/**
 * API Documentation interactive features
 * Handles expand/collapse toggles, scroll state, and find-in-page support
 * for both OperationView and SchemaView pages.
 */
import { decorateApiCodeTokens } from './api-code-tokens'
import { applyParamSummaryFit } from './api-param-summary'

// Check if hidden="until-found" is supported (for find-in-page in collapsed sections)
const supportsHiddenUntilFound = 'onbeforematch' in document.body

function setDisclosureToggle(
    toggleBtn: Element | null,
    expanded: boolean,
    noun: string
): void {
    if (!toggleBtn) return
    toggleBtn.setAttribute('aria-expanded', expanded ? 'true' : 'false')
    const icon = toggleBtn.querySelector('.toggle-icon')
    const label = toggleBtn.querySelector('.toggle-label')
    if (icon) icon.textContent = expanded ? '−' : '+'
    if (label) label.textContent = `${expanded ? 'hide' : 'show'} ${noun}`
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

function expandResponsePanel(panel: HTMLElement): void {
    const toggleBtn = panel.querySelector<HTMLButtonElement>(
        ':scope > .response-status-toggle'
    )
    const body = panel.querySelector<HTMLElement>(
        ':scope > .response-panel-body'
    )

    panel.classList.remove('collapsed')
    panel.classList.add('expanded')
    toggleBtn?.setAttribute('aria-expanded', 'true')
    setUntilFoundHidden(body, false)
}

function collapseResponsePanel(panel: HTMLElement): void {
    const toggleBtn = panel.querySelector<HTMLButtonElement>(
        ':scope > .response-status-toggle'
    )
    const body = panel.querySelector<HTMLElement>(
        ':scope > .response-panel-body'
    )

    panel.classList.remove('expanded')
    panel.classList.add('collapsed')
    toggleBtn?.setAttribute('aria-expanded', 'false')
    setUntilFoundHidden(body, true)
}

/**
 * Expand a property item and all its ancestors
 */
function expandPropertyItem(propertyItem: HTMLElement): void {
    if (!propertyItem) return

    const toggleBtn = propertyItem.querySelector<HTMLButtonElement>(
        ':scope > .expand-toggle-row > .expand-toggle'
    )
    const nestedProps = propertyItem.querySelector<HTMLElement>(
        ':scope > .nested-properties'
    )

    propertyItem.classList.remove('collapsed')
    propertyItem.classList.add('expanded')
    setDisclosureToggle(toggleBtn, true, 'properties')

    if (nestedProps) {
        nestedProps.removeAttribute('hidden')
    }

    // Recursively expand parent property items
    const parentItem = propertyItem.parentElement?.closest<HTMLElement>(
        '.property-item, .union-variant-item'
    )
    if (parentItem) {
        if (parentItem.classList.contains('union-variant-item')) {
            expandUnionVariantItem(parentItem)
        } else {
            expandPropertyItem(parentItem)
        }
    }

    const responsePanel = propertyItem.closest<HTMLElement>('.response-panel')
    if (responsePanel) expandResponsePanel(responsePanel)
}

/**
 * Expand a union variant item and all its ancestors
 */
function expandUnionVariantItem(variantItem: HTMLElement): void {
    if (!variantItem) return

    const toggleBtn = variantItem.querySelector<HTMLButtonElement>(
        ':scope > .union-expand-toggle > .expand-toggle'
    )
    const nestedProps = variantItem.querySelector<HTMLElement>(
        ':scope > .nested-properties'
    )

    variantItem.classList.remove('collapsed')
    variantItem.classList.add('expanded')
    setDisclosureToggle(toggleBtn, true, 'properties')

    if (nestedProps) {
        nestedProps.removeAttribute('hidden')
    }

    // Recursively expand parent items
    const parentItem = variantItem.parentElement?.closest<HTMLElement>(
        '.property-item, .union-variant-item'
    )
    if (parentItem) {
        if (parentItem.classList.contains('union-variant-item')) {
            expandUnionVariantItem(parentItem)
        } else {
            expandPropertyItem(parentItem)
        }
    }
}

/**
 * Expand a union variants container and all its ancestors
 */
function expandUnionContainer(container: HTMLElement): void {
    if (!container) return

    const toggleBtn = container.querySelector<HTMLButtonElement>(
        ':scope > .union-collapse-toggle > .union-group-toggle'
    )
    const variantsContent = container.querySelector<HTMLElement>(
        ':scope > .union-variants-content'
    )

    container.classList.remove('collapsed')
    container.classList.add('expanded')
    setDisclosureToggle(toggleBtn, true, 'type options')

    if (variantsContent) {
        variantsContent.removeAttribute('hidden')
    }

    // Recursively expand parent items
    const parentItem = container.parentElement?.closest<HTMLElement>(
        '.property-item, .union-variant-item'
    )
    if (parentItem) {
        if (parentItem.classList.contains('union-variant-item')) {
            expandUnionVariantItem(parentItem)
        } else {
            expandPropertyItem(parentItem)
        }
    }
}

function paramSectionBody(section: HTMLElement): HTMLElement | null {
    return section.querySelector<HTMLElement>(
        ':scope > .api-param-section-body'
    )
}

function expandParamSection(section: HTMLElement): void {
    const toggle = section.querySelector<HTMLButtonElement>(
        ':scope > .api-param-section-header > .api-param-section-toggle'
    )
    const body = paramSectionBody(section)

    section.classList.remove('collapsed')
    section.classList.add('expanded')
    toggle?.setAttribute('aria-expanded', 'true')
    body?.removeAttribute('hidden')
}

function collapseParamSection(section: HTMLElement): void {
    const toggle = section.querySelector<HTMLButtonElement>(
        ':scope > .api-param-section-header > .api-param-section-toggle'
    )
    const body = paramSectionBody(section)
    const summary = section.querySelector<HTMLElement>('[data-param-summary]')

    section.classList.remove('expanded')
    section.classList.add('collapsed')
    toggle?.setAttribute('aria-expanded', 'false')
    if (body && supportsHiddenUntilFound)
        body.setAttribute('hidden', 'until-found')
    else body?.setAttribute('hidden', '')

    if (summary) requestAnimationFrame(() => applyParamSummaryFit(summary))
}

let paramSummaryObserver: ResizeObserver | null = null
let paramHashListenerBound = false

function initParamSummaries(): void {
    paramSummaryObserver?.disconnect()
    paramSummaryObserver = new ResizeObserver((entries) => {
        for (const entry of entries)
            applyParamSummaryFit(entry.target as HTMLElement)
    })

    document
        .querySelectorAll<HTMLElement>('[data-param-summary]')
        .forEach((summary) => {
            applyParamSummaryFit(summary)
            paramSummaryObserver?.observe(summary)
        })

    if (!paramHashListenerBound) {
        paramHashListenerBound = true
        window.addEventListener('hashchange', expandParamSectionForHash)
    }
}

function expandParamSectionForHash(): void {
    const id = window.location.hash.slice(1)
    if (!id) return
    const target = document.getElementById(id)
    const section = target?.closest<HTMLElement>('[data-param-section]')
    if (section) expandParamSection(section)
    if (section) expandParamSection(section)
    const panel = target?.closest<HTMLElement>('.response-panel')
    if (panel) expandResponsePanel(panel)
}

/**
 * Initialize API docs for OperationView pages
 */
function initOperationView(section: HTMLElement): void {
    // Add beforematch event listeners for hidden="until-found" elements
    // When find-in-page matches content inside collapsed sections, expand them
    if (supportsHiddenUntilFound) {
        section
            .querySelectorAll<HTMLElement>(
                '.nested-properties[hidden="until-found"]'
            )
            .forEach((nestedProps) => {
                nestedProps.addEventListener('beforematch', function () {
                    const parentItem = nestedProps.parentElement
                    if (parentItem?.classList.contains('union-variant-item')) {
                        expandUnionVariantItem(parentItem)
                    } else if (
                        parentItem?.classList.contains('property-item')
                    ) {
                        expandPropertyItem(parentItem)
                    }
                })
            })

        // Add beforematch event listeners for union variants content
        section
            .querySelectorAll<HTMLElement>(
                '.union-variants-content[hidden="until-found"]'
            )
            .forEach((variantsContent) => {
                variantsContent.addEventListener('beforematch', function () {
                    const container = variantsContent.parentElement
                    if (
                        container?.classList.contains(
                            'union-variants-container'
                        )
                    ) {
                        expandUnionContainer(container)
                    }
                })
            })

        section
            .querySelectorAll<HTMLElement>(
                '.response-panel-body[hidden="until-found"]'
            )
            .forEach((panelBody) => {
                panelBody.addEventListener('beforematch', function () {
                    const panel = panelBody.parentElement
                    if (panel?.classList.contains('response-panel')) {
                        expandResponsePanel(panel)
                    }
                })
            })

        section
            .querySelectorAll<HTMLElement>(
                '.api-param-section-body[hidden="until-found"]'
            )
            .forEach((body) => {
                body.addEventListener('beforematch', function () {
                    const container = body.closest<HTMLElement>(
                        '[data-param-section]'
                    )
                    if (container) expandParamSection(container)
                })
            })
    }

    // Examples jump button visibility
    const examplesBtn = document.getElementById('examples-jump-btn')
    const examplesSection = section.querySelector(
        'h3[data-section="request-examples"], h3[data-section="response-examples"]'
    )

    function updateExamplesButtonVisibility(): void {
        if (!examplesBtn || !examplesSection) return

        const examplesTop = examplesSection.getBoundingClientRect().top
        const viewportHeight = window.innerHeight

        // Show button when examples are below the fold (not visible yet)
        if (examplesTop > viewportHeight) {
            examplesBtn.classList.add('visible')
        } else {
            examplesBtn.classList.remove('visible')
        }
    }

    // Throttled scroll handler
    let ticking = false
    window.addEventListener('scroll', function () {
        if (!ticking) {
            window.requestAnimationFrame(function () {
                updateExamplesButtonVisibility()
                ticking = false
            })
            ticking = true
        }
    })

    // Initial check
    updateExamplesButtonVisibility()

    // Click handler for OperationView-specific elements
    section.addEventListener('click', function (e) {
        const target = e.target as HTMLElement

        // Handle union group toggle buttons (collapse/expand all union options)
        const unionGroupToggle = target.closest<HTMLButtonElement>(
            '.union-group-toggle'
        )
        if (unionGroupToggle) {
            e.preventDefault()
            e.stopPropagation()

            const container = unionGroupToggle.closest<HTMLElement>(
                '.union-variants-container'
            )
            if (!container) return

            const isExpanded = container.classList.contains('expanded')
            const variantsContent = container.querySelector<HTMLElement>(
                ':scope > .union-variants-content'
            )

            if (isExpanded) {
                container.classList.remove('expanded')
                container.classList.add('collapsed')
                setDisclosureToggle(unionGroupToggle, false, 'type options')
                if (variantsContent && supportsHiddenUntilFound) {
                    variantsContent.setAttribute('hidden', 'until-found')
                }
            } else {
                container.classList.remove('collapsed')
                container.classList.add('expanded')
                setDisclosureToggle(unionGroupToggle, true, 'type options')
                if (variantsContent) {
                    variantsContent.removeAttribute('hidden')
                }
            }
            return
        }

        // Handle union variant expand/collapse
        const toggleBtn = target.closest<HTMLButtonElement>('.expand-toggle')
        if (toggleBtn) {
            const unionToggleRow = toggleBtn.closest('.union-expand-toggle')
            if (unionToggleRow) {
                e.preventDefault()
                e.stopPropagation()

                const unionVariantItem = toggleBtn.closest<HTMLElement>(
                    '.union-variant-item'
                )
                if (!unionVariantItem) return

                const isExpanded =
                    unionVariantItem.classList.contains('expanded')
                const nestedProps = unionVariantItem.querySelector<HTMLElement>(
                    ':scope > .nested-properties'
                )

                if (isExpanded) {
                    unionVariantItem.classList.remove('expanded')
                    unionVariantItem.classList.add('collapsed')
                    setDisclosureToggle(toggleBtn, false, 'properties')
                    if (nestedProps && supportsHiddenUntilFound) {
                        nestedProps.setAttribute('hidden', 'until-found')
                    }
                } else {
                    unionVariantItem.classList.remove('collapsed')
                    unionVariantItem.classList.add('expanded')
                    setDisclosureToggle(toggleBtn, true, 'properties')
                    if (nestedProps) {
                        nestedProps.removeAttribute('hidden')
                    }
                }
            }
        }
    })
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

    document.addEventListener('click', function (e) {
        const target = e.target as HTMLElement

        // Only handle clicks within API doc sections
        const apiSection = target.closest(
            '#elastic-api-v3, #schema-definition'
        ) as HTMLElement
        if (!apiSection) return

        const paramSectionToggle = target.closest<HTMLButtonElement>(
            '.api-param-section-toggle'
        )
        if (paramSectionToggle) {
            e.preventDefault()
            e.stopPropagation()
            const section = paramSectionToggle.closest<HTMLElement>(
                '[data-param-section]'
            )
            if (!section) return
            if (section.classList.contains('expanded'))
                collapseParamSection(section)
            else expandParamSection(section)
            return
        }

        const responseStatusToggle = target.closest<HTMLButtonElement>(
            '.response-status-toggle'
        )
        if (responseStatusToggle) {
            e.preventDefault()
            e.stopPropagation()

            const panel =
                responseStatusToggle.closest<HTMLElement>('.response-panel')
            if (!panel) return

            if (panel.classList.contains('expanded'))
                collapseResponsePanel(panel)
            else expandResponsePanel(panel)
            return
        }

        // Handle union group toggle buttons (collapse/expand all union options)
        const unionGroupToggle = target.closest<HTMLButtonElement>(
            '.union-group-toggle'
        )
        if (unionGroupToggle) {
            e.preventDefault()
            e.stopPropagation()

            const container = unionGroupToggle.closest<HTMLElement>(
                '.union-variants-container'
            )
            if (!container) return

            const isExpanded = container.classList.contains('expanded')
            const variantsContent = container.querySelector<HTMLElement>(
                ':scope > .union-variants-content'
            )

            if (isExpanded) {
                container.classList.remove('expanded')
                container.classList.add('collapsed')
                setDisclosureToggle(unionGroupToggle, false, 'type options')
                if (variantsContent && supportsHiddenUntilFound) {
                    variantsContent.setAttribute('hidden', 'until-found')
                }
            } else {
                container.classList.remove('collapsed')
                container.classList.add('expanded')
                setDisclosureToggle(unionGroupToggle, true, 'type options')
                if (variantsContent) {
                    variantsContent.removeAttribute('hidden')
                }
            }
            return
        }

        // Handle union variant expand/collapse
        const toggleBtn = target.closest<HTMLButtonElement>('.expand-toggle')
        if (toggleBtn) {
            const unionToggleRow = toggleBtn.closest('.union-expand-toggle')
            if (unionToggleRow) {
                e.preventDefault()
                e.stopPropagation()

                const unionVariantItem = toggleBtn.closest<HTMLElement>(
                    '.union-variant-item'
                )
                if (!unionVariantItem) return

                const isExpanded =
                    unionVariantItem.classList.contains('expanded')
                const nestedProps = unionVariantItem.querySelector<HTMLElement>(
                    ':scope > .nested-properties'
                )

                if (isExpanded) {
                    unionVariantItem.classList.remove('expanded')
                    unionVariantItem.classList.add('collapsed')
                    setDisclosureToggle(toggleBtn, false, 'properties')
                    if (nestedProps && supportsHiddenUntilFound) {
                        nestedProps.setAttribute('hidden', 'until-found')
                    }
                } else {
                    unionVariantItem.classList.remove('collapsed')
                    unionVariantItem.classList.add('expanded')
                    setDisclosureToggle(toggleBtn, true, 'properties')
                    if (nestedProps) {
                        nestedProps.removeAttribute('hidden')
                    }
                }
                return
            }

            // Handle property item expand/collapse toggle buttons
            // Skip if this is a union toggle (already handled above)
            if (toggleBtn.closest('.union-group-toggle')) return

            e.preventDefault()
            e.stopPropagation()

            const propertyItem =
                toggleBtn.closest<HTMLElement>('.property-item')
            if (!propertyItem) return

            const isExpanded = propertyItem.classList.contains('expanded')
            const nestedProps = propertyItem.querySelector<HTMLElement>(
                ':scope > .nested-properties'
            )

            if (isExpanded) {
                propertyItem.classList.remove('expanded')
                propertyItem.classList.add('collapsed')
                setDisclosureToggle(toggleBtn, false, 'properties')
                if (nestedProps && supportsHiddenUntilFound) {
                    nestedProps.setAttribute('hidden', 'until-found')
                }
            } else {
                propertyItem.classList.remove('collapsed')
                propertyItem.classList.add('expanded')
                setDisclosureToggle(toggleBtn, true, 'properties')
                if (nestedProps) {
                    nestedProps.removeAttribute('hidden')
                }
            }
        }
    })
}

const apiLanguageStorageKey = 'tab-id-api-language'

function apiSelectOptions(dropdown: Element): HTMLElement[] {
    return Array.from(
        dropdown.querySelectorAll<HTMLElement>('[role="option"][data-value]')
    )
}

function apiSelectValue(dropdown: Element): string | undefined {
    return dropdown.querySelector<HTMLElement>(
        '[role="option"][aria-selected="true"]'
    )?.dataset.value
}

function setApiSelectValue(dropdown: Element, value: string): boolean {
    const options = apiSelectOptions(dropdown)
    const match = options.find((option) => option.dataset.value === value)
    if (!match) return false

    options.forEach((option) => {
        const selected = option === match
        option.classList.toggle('is-selected', selected)
        option.setAttribute('aria-selected', selected ? 'true' : 'false')
    })

    const label = dropdown.querySelector<HTMLElement>('.api-select-value')
    if (label) label.textContent = match.textContent?.trim() ?? value
    return true
}

function closeApiSelect(option: HTMLElement): void {
    const dropdown = option.closest<HTMLDetailsElement>('details.api-select')
    if (dropdown) dropdown.open = false
}

function applyApiCodeLanguage(
    root: ParentNode,
    language: string,
    persist: boolean
): void {
    root.querySelectorAll<HTMLElement>('[data-api-code-sample]').forEach(
        (widget) => {
            const panels = Array.from(
                widget.querySelectorAll<HTMLElement>('.api-code-sample-panel')
            )
            const dropdown = widget.querySelector('.api-code-sample-lang')
            const hasLanguage = panels.some(
                (panel) => panel.dataset.lang === language
            )
            // Examples that only ship JSON (or another single sample) keep that
            // body. Applying a language they don't have hid every panel and left
            // a header-only card.
            let effectiveLanguage = language
            if (!hasLanguage) {
                effectiveLanguage =
                    (dropdown ? apiSelectValue(dropdown) : undefined) ??
                    panels.find((panel) => !panel.hasAttribute('hidden'))
                        ?.dataset.lang ??
                    panels[0]?.dataset.lang ??
                    language
            } else if (dropdown) {
                setApiSelectValue(dropdown, language)
            }

            panels.forEach((panel) => {
                panel.toggleAttribute(
                    'hidden',
                    panel.dataset.lang !== effectiveLanguage
                )
            })

            widget
                .querySelectorAll<HTMLButtonElement>(
                    '.api-code-sample-actions .copybtn--in-header'
                )
                .forEach((button) => {
                    button.hidden = button.dataset.lang !== effectiveLanguage
                })
        }
    )

    if (persist) {
        window.sessionStorage.setItem(apiLanguageStorageKey, language)
    }
}

let apiCodeLanguageSelectDelegated = false

/**
 * Language picker in API code-sample headers. Persists via sessionStorage.
 */
export function initApiCodeLanguageSelects(): void {
    const dropdowns = document.querySelectorAll('.api-code-sample-lang')
    if (dropdowns.length === 0) return

    const saved = window.sessionStorage.getItem(apiLanguageStorageKey)
    if (saved) {
        applyApiCodeLanguage(document, saved, false)
    }

    if (apiCodeLanguageSelectDelegated) return
    apiCodeLanguageSelectDelegated = true
    document.addEventListener('click', (event) => {
        const option = (event.target as HTMLElement | null)?.closest(
            '.api-code-sample-lang [role="option"][data-value]'
        )
        if (!(option instanceof HTMLElement) || !option.dataset.value) return
        closeApiSelect(option)
        applyApiCodeLanguage(document, option.dataset.value, true)
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

    widget
        .querySelectorAll<HTMLButtonElement>(
            '.example-block-actions .copybtn--in-header'
        )
        .forEach((button) => {
            button.hidden = button.dataset.status !== effective
        })
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
        if (widget) applyApiResponseStatus(widget, tab.dataset.status)
    })
}

function applyApiScenario(widget: HTMLElement, scenarioId: string): void {
    widget
        .querySelectorAll<HTMLElement>(
            '.api-examples-scenario-panel[data-scenario]'
        )
        .forEach((panel) => {
            const match = panel.dataset.scenario === scenarioId
            panel.toggleAttribute('hidden', !match)
        })

    widget
        .querySelectorAll('.api-scenario-select')
        .forEach((dropdown) => setApiSelectValue(dropdown, scenarioId))
}

let apiScenarioSelectDelegated = false

/**
 * Scenario picker in the Examples header card. Switches the request+response
 * pair. Not persisted — scenario ids/titles differ per operation.
 */
export function initApiScenarioSelects(): void {
    // Always register delegation once — pickers may appear after HTMX navigation.
    if (apiScenarioSelectDelegated) return
    apiScenarioSelectDelegated = true
    document.addEventListener('click', (event) => {
        const option = (event.target as HTMLElement | null)?.closest(
            '.api-scenario-select [role="option"][data-value]'
        )
        if (!(option instanceof HTMLElement) || !option.dataset.value) return
        const widget = option.closest<HTMLElement>('[data-api-scenarios]')
        closeApiSelect(option)
        if (widget) applyApiScenario(widget, option.dataset.value)
    })
}

function countApiCodeLines(text: string): number {
    if (!text) return 1
    const parts = text.split(/\r?\n/)
    if (parts[parts.length - 1] === '') parts.pop()
    return Math.max(1, parts.length)
}

/**
 * Add a non-selectable line-number gutter beside request/response code in the
 * examples rail. Uses a sibling <pre> (same font metrics as the code) so numbers
 * stay aligned and mouse selection / copy omit them.
 */
function initApiCodeLineNumbers(): void {
    const panel = document.getElementById('api-examples-panel')
    if (!panel) return

    panel
        .querySelectorAll<HTMLElement>('.api-code-card pre code')
        .forEach((code) => {
            const pre = code.parentElement
            if (!(pre instanceof HTMLPreElement)) return
            if (pre.parentElement?.classList.contains('api-code-lines')) return
            if (pre.classList.contains('api-code-line-gutter')) return

            const lineCount = countApiCodeLines(code.textContent ?? '')
            const wrapper = document.createElement('div')
            wrapper.className = 'api-code-lines'
            const gutter = document.createElement('pre')
            gutter.className = 'api-code-line-gutter'
            gutter.setAttribute('aria-hidden', 'true')
            gutter.textContent = Array.from({ length: lineCount }, (_, index) =>
                String(index + 1)
            ).join('\n')

            // Match code metrics so gutter rows stay 1:1 with content rows
            const codeStyle = getComputedStyle(code)
            gutter.style.fontFamily = codeStyle.fontFamily
            gutter.style.fontSize = codeStyle.fontSize
            gutter.style.lineHeight = codeStyle.lineHeight
            gutter.style.fontWeight = codeStyle.fontWeight
            gutter.style.paddingTop = codeStyle.paddingTop
            gutter.style.paddingBottom = codeStyle.paddingBottom

            pre.replaceWith(wrapper)
            wrapper.append(gutter, pre)
        })
}

const apiEndpointCopyIcon = `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
		<path fill-rule="evenodd" clip-rule="evenodd" d="M6 1C5.44771 1 5 1.44772 5 2V10C5 10.5523 5.44772 11 6 11H14C14.5523 11 15 10.5523 15 10V2C15 1.44771 14.5523 1 14 1H6ZM6 2L14 2V10H6V2Z" fill="currentColor"/>
		<path d="M2 5H4V6H2V14H10V12H11V14C11 14.5523 10.5523 15 10 15H2C1.44772 15 1 14.5523 1 14V6C1 5.44772 1.44771 5 2 5Z" fill="currentColor"/>
	</svg>`

const apiEndpointCheckIcon = `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
  <path fill="currentColor" fill-rule="evenodd" d="M6.5 12.242 2.354 8.096l.707-.707L6.5 10.828l6.44-6.44.707.708-7.147 7.146Z"/>
</svg>`

let apiEndpointCopyInitialized = false
let apiPageActionsInitialized = false

function closestEndpointCopyButton(
    target: EventTarget | null
): HTMLButtonElement | null {
    if (!(target instanceof Element)) return null
    return target.closest('button.api-url-copy')
}

function initApiEndpointCopy(): void {
    if (apiEndpointCopyInitialized) return
    apiEndpointCopyInitialized = true

    document.addEventListener(
        'mousedown',
        (e) => {
            if (!closestEndpointCopyButton(e.target)) return
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
                    btn.setAttribute('data-tooltip', 'Copied!')
                    btn.innerHTML = apiEndpointCheckIcon
                    window.setTimeout(() => {
                        btn.classList.remove('success')
                        btn.setAttribute('data-tooltip', 'Copy')
                        btn.innerHTML = apiEndpointCopyIcon
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

/**
 * Initialize API documentation interactivity
 * Call this after page load or HTMX content swap
 */
export function initApiDocs(): void {
    // Initialize global click handlers once (uses event delegation)
    initGlobalClickHandlers()
    initApiEndpointCopy()
    initApiCodeLanguageSelects()
    initApiResponseStatusTabs()
    initApiScenarioSelects()
    initApiPageActions()
    // After initHighlight — gutters need final textContent line counts
    decorateApiCodeTokens()
    initApiCodeLineNumbers()

    // Check for OperationView page - initialize view-specific features
    initParamSummaries()

    const operationSection = document.getElementById('elastic-api-v3')
    if (operationSection) {
        initOperationView(operationSection)
        expandParamSectionForHash()
    }
}
