/**
 * API Documentation interactive features
 * Handles expand/collapse toggles, scroll state, and find-in-page support
 * for both OperationView and SchemaView pages.
 */
import { initApiBreadcrumbs } from './api-breadcrumbs'
import { decorateApiCodeTokens } from './api-code-tokens'
import { initApiExamples } from './api-examples-carousel'
import { applyParamSummaryFit } from './api-param-summary'
import { iconCheckEui, iconCopyEui } from './copybutton'
import { initTooltips } from './tooltip'

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

function expandResponsePanel(panel: HTMLElement): void {
    const toggleBtn = panel.querySelector<HTMLButtonElement>(
        'button.response-status-toggle'
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
        'button.response-status-toggle'
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
        const toggleBtn = target.closest<HTMLButtonElement>(
            'button.expand-toggle'
        )
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

    // Find-in-page matched a folded enum literal: unfold the row so the match is visible
    document.addEventListener('beforematch', function (e) {
        const folded = (e.target as HTMLElement).closest('.enum-values-folded')
        const toggleBtn =
            folded?.parentElement?.querySelector<HTMLButtonElement>(
                ':scope > .enum-values-toggle'
            )
        if (toggleBtn) setEnumValuesExpanded(toggleBtn, true)
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

        // The title toggles like the rest of the row. pushState sets the hash without
        // a hashchange, which would otherwise re-expand a section the click just collapsed.
        const paramSectionTitle = target.closest<HTMLAnchorElement>(
            '.api-param-section-header > .headerlink'
        )
        if (paramSectionTitle) {
            // Modified clicks keep normal link behaviour (new tab, new window)
            if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return
            e.preventDefault()
            const section = paramSectionTitle.closest<HTMLElement>(
                '[data-param-section]'
            )
            if (!section) return
            if (section.classList.contains('expanded'))
                collapseParamSection(section)
            else expandParamSection(section)
            if (window.location.hash !== paramSectionTitle.hash)
                history.pushState(null, '', paramSectionTitle.hash)
            return
        }

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

        const responsePanel = target
            .closest('button.response-status-toggle')
            ?.closest<HTMLElement>('.response-panel')
        if (responsePanel) {
            // The toggle's hit area covers the description, so selecting its text must not toggle.
            const summary = responsePanel.querySelector('.response-summary')
            const selection = window.getSelection()
            if (
                selection &&
                !selection.isCollapsed &&
                (summary?.contains(selection.anchorNode) ||
                    summary?.contains(selection.focusNode))
            )
                return

            e.preventDefault()
            e.stopPropagation()

            if (responsePanel.classList.contains('expanded'))
                collapseResponsePanel(responsePanel)
            else expandResponsePanel(responsePanel)
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
        const toggleBtn = target.closest<HTMLButtonElement>(
            'button.expand-toggle'
        )
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
        if (widget) applyApiResponseStatus(widget, tab.dataset.status)
    })
}

let apiEndpointCopyInitialized = false
let apiPageActionsInitialized = false

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
                    btn.setAttribute('data-tooltip', 'Copied!')
                    btn.innerHTML = iconCheckEui
                    window.setTimeout(() => {
                        btn.classList.remove('success')
                        btn.setAttribute('data-tooltip', 'Copy')
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
    initTooltips()
    initApiEndpointCopy()
    initApiResponseStatusTabs()
    initApiExamples()
    initApiPageActions()
    initApiBreadcrumbs()
    // After initHighlight — gutters need final textContent line counts
    decorateApiCodeTokens()

    // Check for OperationView page - initialize view-specific features
    initParamSummaries()

    const operationSection = document.getElementById('elastic-api-v3')
    if (operationSection) {
        initOperationView(operationSection)
        expandParamSectionForHash()
    }
}
