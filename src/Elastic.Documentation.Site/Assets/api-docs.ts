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
        if (toggleLabel) toggleLabel.textContent = `Hide ${propCount} properties`
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
        if (toggleLabel) toggleLabel.textContent = `Hide ${propCount} properties`
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
                        container?.classList.contains('union-variants-container')
                    ) {
                        expandUnionContainer(container)
                    }
                })
            })
    }

    // Scroll detection for section-path visibility
    let triggerElement: Element | null = null

    // First check for Path Parameters header
    const pathParamsHeader = section.querySelector('h4')
    if (
        pathParamsHeader &&
        pathParamsHeader.textContent?.includes('Path Parameters')
    ) {
        triggerElement = pathParamsHeader
    } else {
        // Fall back to the paths section URL listing
        const pathsHeader = section.querySelector('h3[data-section="paths"]')
        if (pathsHeader) {
            triggerElement = pathsHeader.nextElementSibling
        }
    }

    function updateScrollState(): void {
        if (!triggerElement) return

        const triggerBottom = triggerElement.getBoundingClientRect().bottom
        const isScrolled = triggerBottom < 100 // 100px from top of viewport

        section.querySelectorAll('h3.section-header').forEach((header) => {
            if (isScrolled) {
                header.classList.add('scrolled')
            } else {
                header.classList.remove('scrolled')
            }
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
                updateScrollState()
                updateExamplesButtonVisibility()
                ticking = false
            })
            ticking = true
        }
    })

    // Initial check
    updateScrollState()
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

        // Handle section navigation buttons
        const navBtn = target.closest<HTMLButtonElement>('.section-nav-btn')
        if (navBtn) {
            e.preventDefault()
            e.stopPropagation()

            const direction = navBtn.getAttribute('data-dir')
            const headers = Array.from(
                section.querySelectorAll('h3.section-header')
            )
            const currentHeader = navBtn.closest('h3')
            const currentIndex = headers.indexOf(currentHeader as HTMLElement)

            if (currentIndex === -1) return

            let targetIndex: number
            if (direction === 'up') {
                targetIndex =
                    currentIndex > 0 ? currentIndex - 1 : headers.length - 1
            } else {
                targetIndex =
                    currentIndex < headers.length - 1 ? currentIndex + 1 : 0
            }

            const targetHeader = headers[targetIndex]
            if (targetHeader) {
                const offset = 80
                const targetPosition =
                    targetHeader.getBoundingClientRect().top +
                    window.scrollY -
                    offset
                window.scrollTo({ top: targetPosition, behavior: 'smooth' })
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

                const unionVariantItem =
                    toggleBtn.closest<HTMLElement>('.union-variant-item')
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

/**
 * Initialize common expand/collapse functionality for property lists
 * Works for both OperationView and SchemaView
 */
function initPropertyListToggle(section: HTMLElement): void {
    section.addEventListener('click', function (e) {
        const target = e.target as HTMLElement

        // Handle expand/collapse toggle buttons
        const toggleBtn = target.closest<HTMLButtonElement>('.expand-toggle')
        if (toggleBtn) {
            // Skip if this is a union expand toggle (handled by OperationView-specific code)
            if (toggleBtn.closest('.union-expand-toggle')) return
            // Skip if this is a union group toggle
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

/**
 * Initialize API documentation interactivity
 * Call this after page load or HTMX content swap
 */
export function initApiDocs(): void {
    // Check for OperationView page
    const operationSection = document.getElementById('elastic-api-v3')
    if (operationSection) {
        initOperationView(operationSection)
        initPropertyListToggle(operationSection)
        return
    }

    // Check for SchemaView page
    const schemaSection = document.getElementById('schema-definition')
    if (schemaSection) {
        initPropertyListToggle(schemaSection)
    }
}
