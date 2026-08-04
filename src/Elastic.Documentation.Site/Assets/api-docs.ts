/**
 * API Documentation interactive features
 * Handles expand/collapse toggles, scroll state, and find-in-page support
 * for both OperationView and SchemaView pages.
 */

// Check if hidden="until-found" is supported (for find-in-page in collapsed sections)
const supportsHiddenUntilFound = 'onbeforematch' in document.body

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

    if (toggleBtn) {
        toggleBtn.setAttribute('aria-expanded', 'true')
        const toggleIcon = toggleBtn.querySelector('.toggle-icon')
        const toggleLabel = toggleBtn.querySelector('.toggle-label')
        const propCount = toggleLabel?.textContent?.match(/\d+/)?.[0] || ''
        if (toggleIcon) toggleIcon.textContent = '−'
        if (toggleLabel)
            toggleLabel.textContent = `Hide ${propCount} properties`
    }

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

    if (toggleBtn) {
        const toggleIcon = toggleBtn.querySelector('.toggle-icon')
        const toggleLabel = toggleBtn.querySelector('.toggle-label')
        const propCount = toggleLabel?.textContent?.match(/\d+/)?.[0] || ''
        if (toggleIcon) toggleIcon.textContent = '−'
        if (toggleLabel)
            toggleLabel.textContent = `Hide ${propCount} properties`
    }

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

    if (toggleBtn) {
        const toggleIcon = toggleBtn.querySelector('.toggle-icon')
        const toggleLabel = toggleBtn.querySelector('.toggle-label')
        const optionCount = toggleLabel?.textContent?.match(/\d+/)?.[0] || ''
        if (toggleIcon) toggleIcon.textContent = '−'
        if (toggleLabel)
            toggleLabel.textContent = `Hide ${optionCount} type options`
    }

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
            const toggleIcon = unionGroupToggle.querySelector('.toggle-icon')
            const toggleLabel = unionGroupToggle.querySelector('.toggle-label')
            const variantsContent = container.querySelector<HTMLElement>(
                ':scope > .union-variants-content'
            )
            const optionCount =
                toggleLabel?.textContent?.match(/\d+/)?.[0] || ''

            if (isExpanded) {
                container.classList.remove('expanded')
                container.classList.add('collapsed')
                if (toggleIcon) toggleIcon.textContent = '+'
                if (toggleLabel)
                    toggleLabel.textContent = `Show ${optionCount} type options`
                if (variantsContent && supportsHiddenUntilFound) {
                    variantsContent.setAttribute('hidden', 'until-found')
                }
            } else {
                container.classList.remove('collapsed')
                container.classList.add('expanded')
                if (toggleIcon) toggleIcon.textContent = '−'
                if (toggleLabel)
                    toggleLabel.textContent = `Hide ${optionCount} type options`
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
                const toggleIcon = toggleBtn.querySelector('.toggle-icon')
                const toggleLabel = toggleBtn.querySelector('.toggle-label')
                const nestedProps = unionVariantItem.querySelector<HTMLElement>(
                    ':scope > .nested-properties'
                )
                const propCount =
                    toggleLabel?.textContent?.match(/\d+/)?.[0] || ''

                if (isExpanded) {
                    unionVariantItem.classList.remove('expanded')
                    unionVariantItem.classList.add('collapsed')
                    if (toggleIcon) toggleIcon.textContent = '+'
                    if (toggleLabel)
                        toggleLabel.textContent = `Show ${propCount} properties`
                    if (nestedProps && supportsHiddenUntilFound) {
                        nestedProps.setAttribute('hidden', 'until-found')
                    }
                } else {
                    unionVariantItem.classList.remove('collapsed')
                    unionVariantItem.classList.add('expanded')
                    if (toggleIcon) toggleIcon.textContent = '−'
                    if (toggleLabel)
                        toggleLabel.textContent = `Hide ${propCount} properties`
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
            const toggleIcon = unionGroupToggle.querySelector('.toggle-icon')
            const toggleLabel = unionGroupToggle.querySelector('.toggle-label')
            const variantsContent = container.querySelector<HTMLElement>(
                ':scope > .union-variants-content'
            )
            const optionCount =
                toggleLabel?.textContent?.match(/\d+/)?.[0] || ''

            if (isExpanded) {
                container.classList.remove('expanded')
                container.classList.add('collapsed')
                if (toggleIcon) toggleIcon.textContent = '+'
                if (toggleLabel)
                    toggleLabel.textContent = `Show ${optionCount} type options`
                if (variantsContent && supportsHiddenUntilFound) {
                    variantsContent.setAttribute('hidden', 'until-found')
                }
            } else {
                container.classList.remove('collapsed')
                container.classList.add('expanded')
                if (toggleIcon) toggleIcon.textContent = '−'
                if (toggleLabel)
                    toggleLabel.textContent = `Hide ${optionCount} type options`
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
                const toggleIcon = toggleBtn.querySelector('.toggle-icon')
                const toggleLabel = toggleBtn.querySelector('.toggle-label')
                const nestedProps = unionVariantItem.querySelector<HTMLElement>(
                    ':scope > .nested-properties'
                )
                const propCount =
                    toggleLabel?.textContent?.match(/\d+/)?.[0] || ''

                if (isExpanded) {
                    unionVariantItem.classList.remove('expanded')
                    unionVariantItem.classList.add('collapsed')
                    if (toggleIcon) toggleIcon.textContent = '+'
                    if (toggleLabel)
                        toggleLabel.textContent = `Show ${propCount} properties`
                    if (nestedProps && supportsHiddenUntilFound) {
                        nestedProps.setAttribute('hidden', 'until-found')
                    }
                } else {
                    unionVariantItem.classList.remove('collapsed')
                    unionVariantItem.classList.add('expanded')
                    if (toggleIcon) toggleIcon.textContent = '−'
                    if (toggleLabel)
                        toggleLabel.textContent = `Hide ${propCount} properties`
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
            const toggleIcon = toggleBtn.querySelector('.toggle-icon')
            const toggleLabel = toggleBtn.querySelector('.toggle-label')
            const nestedProps = propertyItem.querySelector<HTMLElement>(
                ':scope > .nested-properties'
            )
            const propCount = toggleLabel?.textContent?.match(/\d+/)?.[0] || ''

            if (isExpanded) {
                propertyItem.classList.remove('expanded')
                propertyItem.classList.add('collapsed')
                toggleBtn.setAttribute('aria-expanded', 'false')
                if (toggleIcon) toggleIcon.textContent = '+'
                if (toggleLabel)
                    toggleLabel.textContent = `Show ${propCount} properties`
                // Set hidden="until-found" for find-in-page searchability
                if (nestedProps && supportsHiddenUntilFound) {
                    nestedProps.setAttribute('hidden', 'until-found')
                }
            } else {
                propertyItem.classList.remove('collapsed')
                propertyItem.classList.add('expanded')
                toggleBtn.setAttribute('aria-expanded', 'true')
                if (toggleIcon) toggleIcon.textContent = '−'
                if (toggleLabel)
                    toggleLabel.textContent = `Hide ${propCount} properties`
                // Remove hidden attribute when expanding
                if (nestedProps) {
                    nestedProps.removeAttribute('hidden')
                }
            }
        }
    })
}

const apiLanguageStorageKey = 'tab-id-api-language'

function applyApiCodeLanguage(
    root: ParentNode,
    language: string,
    persist: boolean
): void {
    root.querySelectorAll<HTMLElement>('[data-api-code-sample]').forEach(
        (widget) => {
            const select = widget.querySelector<HTMLSelectElement>(
                '.api-code-sample-lang'
            )
            let effectiveLanguage = language
            if (select) {
                const hasOption = Array.from(select.options).some(
                    (option) => option.value === language
                )
                if (hasOption) select.value = language
                effectiveLanguage = select.value
            }

            const label = widget.querySelector('.api-code-sample-label')
            if (label) label.textContent = effectiveLanguage

            widget
                .querySelectorAll<HTMLElement>('.api-code-sample-panel')
                .forEach((panel) => {
                    const match = panel.dataset.lang === effectiveLanguage
                    panel.toggleAttribute('hidden', !match)
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
 * Language <select> in API code-sample headers. Persists via sessionStorage.
 */
function initApiCodeLanguageSelects(): void {
    const selects = document.querySelectorAll<HTMLSelectElement>(
        '.api-code-sample-lang'
    )
    if (selects.length === 0) return

    const saved = window.sessionStorage.getItem(apiLanguageStorageKey)
    if (saved) {
        applyApiCodeLanguage(document, saved, false)
    }

    if (apiCodeLanguageSelectDelegated) return
    apiCodeLanguageSelectDelegated = true
    document.addEventListener('change', (event) => {
        const select = (event.target as HTMLElement | null)?.closest(
            '.api-code-sample-lang'
        )
        if (select instanceof HTMLSelectElement) {
            applyApiCodeLanguage(document, select.value, true)
        }
    })
}

function applyApiResponseStatus(
    widget: HTMLElement,
    statusCode: string
): void {
    const tabs = Array.from(
        widget.querySelectorAll<HTMLElement>('.example-response-tab[data-status]')
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
    const select = widget.querySelector<HTMLSelectElement>(
        '.api-examples-scenario-select'
    )
    if (select) {
        const hasOption = Array.from(select.options).some(
            (option) => option.value === scenarioId
        )
        if (hasOption) select.value = scenarioId
    }

    widget.querySelectorAll<HTMLElement>('[data-scenario]').forEach((panel) => {
        const match = panel.dataset.scenario === scenarioId
        panel.toggleAttribute('hidden', !match)
    })
}

let apiScenarioSelectDelegated = false

/**
 * Scenario <select> in the examples rail. Switches which example panel is visible.
 * Not persisted — scenario ids/titles differ per operation.
 */
function initApiScenarioSelects(): void {
    // Always register delegation once — selects may appear after HTMX navigation.
    if (apiScenarioSelectDelegated) return
    apiScenarioSelectDelegated = true
    document.addEventListener('change', (event) => {
        const select = (event.target as HTMLElement | null)?.closest(
            '.api-examples-scenario-select'
        )
        if (!(select instanceof HTMLSelectElement)) return
        const widget = select.closest<HTMLElement>('[data-api-scenarios]')
        if (widget) applyApiScenario(widget, select.value)
    })
}

let apiPathOverloadSelectDelegated = false

/**
 * Path-overload <select>: navigate to a sibling route via the same HTMX oob swap
 * used by in-page API links, then reset to the "Other paths (N)" placeholder.
 */
function initApiPathOverloadSelect(): void {
    if (apiPathOverloadSelectDelegated) return
    apiPathOverloadSelectDelegated = true
    document.addEventListener('change', (event) => {
        const select = (event.target as HTMLElement | null)?.closest(
            '.api-path-overload-select'
        )
        if (!(select instanceof HTMLSelectElement) || !select.value) return

        const url = select.value
        const oob =
            select.dataset.hxSelectOob ??
            '#content-container,#toc-nav,#api-examples-panel'
        const htmxApi = (
            window as Window & {
                htmx?: {
                    ajax: (
                        method: string,
                        path: string,
                        context?: Record<string, unknown>
                    ) => void
                    process?: (el: Element) => void
                }
            }
        ).htmx

        if (htmxApi?.ajax) {
            htmxApi.ajax('GET', url, {
                source: select,
                swap: 'none',
                selectOOB: oob,
                push: url,
            })
        } else {
            window.location.assign(url)
        }

        select.selectedIndex = 0
    })
}

function countApiCodeLines(text: string): number {
    if (!text) return 1
    const parts = text.split(/\r?\n/)
    if (parts.at(-1) === '') parts.pop()
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
        .querySelectorAll<HTMLElement>(
            '.api-code-sample pre code, .example-block--response pre code'
        )
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
            gutter.textContent = Array.from(
                { length: lineCount },
                (_, index) => String(index + 1)
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

/**
 * Initialize API documentation interactivity
 * Call this after page load or HTMX content swap
 */
export function initApiDocs(): void {
    // Initialize global click handlers once (uses event delegation)
    initGlobalClickHandlers()
    initApiCodeLanguageSelects()
    initApiResponseStatusTabs()
    initApiScenarioSelects()
    initApiPathOverloadSelect()
    // After initHighlight — gutters need final textContent line counts
    initApiCodeLineNumbers()

    // Check for OperationView page - initialize view-specific features
    const operationSection = document.getElementById('elastic-api-v3')
    if (operationSection) {
        initOperationView(operationSection)
    }
}
