/**
 * Collapse middle API breadcrumbs from the item before the current page
 * when the toolbar row is too narrow. First crumb and current page stay.
 */

export function collapsedMiddleCount(
    availableWidth: number,
    firstWidth: number,
    lastWidth: number,
    overflowWidth: number,
    sepWidth: number,
    middleWidths: number[]
): number {
    if (middleWidths.length === 0 || availableWidth <= 0) return 0

    let visible = middleWidths.length
    while (visible >= 0) {
        const hidden = middleWidths.length - visible
        const shown = middleWidths.slice(0, visible)
        const width = trailWidth(
            firstWidth,
            lastWidth,
            overflowWidth,
            sepWidth,
            shown,
            hidden > 0
        )
        if (width <= availableWidth) return hidden
        if (visible === 0) return middleWidths.length
        visible--
    }

    return middleWidths.length
}

function trailWidth(
    firstWidth: number,
    lastWidth: number,
    overflowWidth: number,
    sepWidth: number,
    visibleMiddles: number[],
    showOverflow: boolean
): number {
    const pieces = 2 + visibleMiddles.length + (showOverflow ? 1 : 0)
    const content =
        firstWidth +
        lastWidth +
        visibleMiddles.reduce((sum, width) => sum + width, 0) +
        (showOverflow ? overflowWidth : 0)
    return content + sepWidth * Math.max(0, pieces - 1)
}

export function applyBreadcrumbCollapse(nav: HTMLElement): void {
    const first = nav.querySelector<HTMLElement>('[data-crumb="start"]')
    const last = nav.querySelector<HTMLElement>('[data-crumb="end"]')
    const overflow = nav.querySelector<HTMLElement>(
        '.api-breadcrumbs__overflow'
    )
    const overflowSep = nav.querySelector<HTMLElement>('[data-overflow-sep]')
    if (!first || !last) return

    const middles = [
        ...nav.querySelectorAll<HTMLElement>('[data-crumb="middle"]'),
    ]
    resetCollapse(middles, nav, overflow, overflowSep)
    if (middles.length === 0 || !overflow) return

    const sep = nav.querySelector<HTMLElement>('[data-sep="after-start"]')
    const hiddenCount = collapsedMiddleCount(
        nav.clientWidth,
        first.offsetWidth,
        last.offsetWidth,
        measureOverflow(overflow),
        sep?.offsetWidth ?? 8,
        middles.map((item) => item.offsetWidth)
    )
    applyHidden(middles, nav, overflow, overflowSep, hiddenCount)
}

function resetCollapse(
    middles: HTMLElement[],
    nav: HTMLElement,
    overflow: HTMLElement | null,
    overflowSep: HTMLElement | null
): void {
    for (const item of middles) {
        item.hidden = false
        sepFor(nav, item)?.removeAttribute('hidden')
    }
    for (const option of nav.querySelectorAll<HTMLElement>(
        '[data-overflow-index]'
    )) {
        option.hidden = true
    }
    if (overflow) overflow.hidden = true
    if (overflowSep) overflowSep.hidden = true
}

function applyHidden(
    middles: HTMLElement[],
    nav: HTMLElement,
    overflow: HTMLElement,
    overflowSep: HTMLElement | null,
    hiddenCount: number
): void {
    if (hiddenCount === 0) return

    overflow.hidden = false
    if (overflowSep) overflowSep.hidden = false

    const start = middles.length - hiddenCount
    for (let i = start; i < middles.length; i++) {
        middles[i].hidden = true
        const sep = sepFor(nav, middles[i])
        if (sep) sep.hidden = true
        const option = nav.querySelector<HTMLElement>(
            `[data-overflow-index="${i}"]`
        )
        if (option) option.hidden = false
    }
}

function sepFor(nav: HTMLElement, item: HTMLElement): HTMLElement | null {
    const index = item.dataset.crumbIndex
    if (index === undefined) return null
    return nav.querySelector<HTMLElement>(`[data-sep-for="${index}"]`)
}

function measureOverflow(overflow: HTMLElement): number {
    const wasHidden = overflow.hidden
    overflow.hidden = false
    const width = overflow.offsetWidth
    overflow.hidden = wasHidden
    return width
}

const observed = new WeakSet<Element>()
const resizeObserver =
    typeof ResizeObserver === 'undefined'
        ? null
        : new ResizeObserver((entries) => {
              for (const entry of entries) {
                  const toolbar =
                      entry.target.closest('.api-page-toolbar') ?? entry.target
                  const nav = toolbar.querySelector<HTMLElement>(
                      '[data-api-breadcrumbs]'
                  )
                  if (nav) applyBreadcrumbCollapse(nav)
              }
          })

export function initApiBreadcrumbs(): void {
    document.querySelectorAll('.api-page-toolbar').forEach((toolbar) => {
        if (resizeObserver && !observed.has(toolbar)) {
            observed.add(toolbar)
            resizeObserver.observe(toolbar)
        }
        const nav = toolbar.querySelector<HTMLElement>('[data-api-breadcrumbs]')
        if (nav) applyBreadcrumbCollapse(nav)
    })
}
