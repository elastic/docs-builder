import { $$optional, $optional } from 'select-dom'

interface HeadingEntry {
    heading: Element
    link: HTMLAnchorElement | null
}
interface CachedHeadingEntry {
    documentTop: number
    link: HTMLAnchorElement | null
}
interface TocItemGeometry {
    item: HTMLLIElement
    top: number
    bottom: number
}
interface TocElements {
    headings: HeadingEntry[]
    tocLinks: HTMLAnchorElement[]
    contentRoot: Element | null
    tocContainer: HTMLDivElement | null
    progressIndicator: HTMLDivElement | null
}
interface TocGeometry {
    headings: CachedHeadingEntry[]
    items: Map<HTMLAnchorElement, TocItemGeometry>
    viewportHeight: number
    maximumScrollPosition: number
}

const HEADING_OFFSET = 34 * 4
const VISIBLE_HEADING_OFFSET = HEADING_OFFSET - 64
const BOTTOM_TOLERANCE = 10
const ACTIVE_CLASS = 'toc-current'
let initializationController: AbortController | null = null

function initializeTocElements(): TocElements {
    const contentRoot = $optional('#markdown-content, #elastic-api-v3') ?? null
    const headingElements = $$optional(
        '#markdown-content h2, #markdown-content h3, #elastic-api-v3 h3[data-section]'
    )
    const tocLinks = $$optional('#toc-nav li>a') as HTMLAnchorElement[]
    const tocContainer = $optional(
        '#toc-nav .toc-progress-container'
    ) as HTMLDivElement
    const progressIndicator = $optional(
        '.toc-progress-indicator',
        tocContainer
    ) as HTMLDivElement | null
    const linksByAnchor = new Map(
        tocLinks.map((link) => [link.getAttribute('href'), link])
    )
    const headings = headingElements.map((heading) => {
        const anchorId = getHeadingAnchorId(heading)
        return {
            heading,
            link: anchorId ? (linksByAnchor.get(`#${anchorId}`) ?? null) : null,
        }
    })
    return { headings, tocLinks, contentRoot, tocContainer, progressIndicator }
}

function getHeadingAnchorId(heading: Element): string | null {
    const dataSection = heading.getAttribute('data-section')
    if (dataSection) return dataSection
    const wrapper = heading.closest('.heading-wrapper')
    return wrapper?.id || heading.id || null
}

function cacheGeometry(elements: TocElements): TocGeometry {
    const scrollY = window.scrollY
    const headings = elements.headings
        .map(({ heading, link }) => {
            const rect = heading.getBoundingClientRect()
            if (rect.width === 0 && rect.height === 0) return null
            return { documentTop: rect.top + scrollY, link }
        })
        .filter((entry): entry is CachedHeadingEntry => entry !== null)
        .sort((left, right) => left.documentTop - right.documentTop)
    const items = new Map<HTMLAnchorElement, TocItemGeometry>()
    const containerTop = elements.tocContainer?.getBoundingClientRect().top ?? 0
    elements.tocLinks.forEach((link) => {
        const item = link.closest('li')
        if (!(item instanceof HTMLLIElement)) return
        const rect = item.getBoundingClientRect()
        items.set(link, {
            item,
            top: rect.top - containerTop,
            bottom: rect.bottom - containerTop,
        })
    })
    const viewportHeight = window.innerHeight
    return {
        headings,
        items,
        viewportHeight,
        maximumScrollPosition: Math.max(
            0,
            document.documentElement.scrollHeight - viewportHeight
        ),
    }
}

function upperBound(headings: CachedHeadingEntry[], position: number): number {
    let low = 0,
        high = headings.length
    while (low < high) {
        const middle = Math.floor((low + high) / 2)
        if (headings[middle].documentTop <= position) low = middle + 1
        else high = middle
    }
    return low
}

function lowerBound(headings: CachedHeadingEntry[], position: number): number {
    let low = 0,
        high = headings.length
    while (low < high) {
        const middle = Math.floor((low + high) / 2)
        if (headings[middle].documentTop < position) low = middle + 1
        else high = middle
    }
    return low
}

function getCurrentLinks(
    headings: CachedHeadingEntry[],
    scrollY: number
): HTMLAnchorElement[] {
    const currentIndex = upperBound(headings, scrollY + HEADING_OFFSET) - 1
    if (currentIndex < 0) return []
    const currentTop = headings[currentIndex].documentTop
    let firstIndex = currentIndex
    while (
        firstIndex > 0 &&
        Math.abs(headings[firstIndex - 1].documentTop - currentTop) <= 1
    )
        firstIndex--
    return headings
        .slice(firstIndex, currentIndex + 1)
        .map(({ link }) => link)
        .filter((link): link is HTMLAnchorElement => link !== null)
}

function getBottomLinks(
    geometry: TocGeometry,
    scrollY: number
): HTMLAnchorElement[] {
    const firstIndex = lowerBound(
        geometry.headings,
        scrollY + VISIBLE_HEADING_OFFSET
    )
    const lastIndex =
        upperBound(geometry.headings, scrollY + geometry.viewportHeight) - 1
    if (lastIndex < firstIndex) return []
    return geometry.headings
        .slice(firstIndex, lastIndex + 1)
        .map(({ link }) => link)
        .filter((link): link is HTMLAnchorElement => link !== null)
}

function linksAreEqual(
    previousLinks: HTMLAnchorElement[],
    currentLinks: HTMLAnchorElement[]
) {
    return (
        previousLinks.length === currentLinks.length &&
        previousLinks.every((link, index) => link === currentLinks[index])
    )
}

function updateIndicator(
    elements: TocElements,
    geometry: TocGeometry,
    links: HTMLAnchorElement[]
) {
    if (!elements.progressIndicator) return
    const activeItems = links
        .map((link) => geometry.items.get(link))
        .filter((item): item is TocItemGeometry => item !== undefined)
    const first = activeItems[0],
        last = activeItems[activeItems.length - 1]
    if (!first || !last) {
        if (elements.progressIndicator.style.height !== '0px')
            elements.progressIndicator.style.height = '0px'
        return
    }
    const transform = `translateY(${first.top}px)`
    const height = `${last.bottom - first.top}px`
    if (elements.progressIndicator.style.transform !== transform)
        elements.progressIndicator.style.transform = transform
    if (elements.progressIndicator.style.height !== height)
        elements.progressIndicator.style.height = height
}

function updateActiveLinks(
    elements: TocElements,
    geometry: TocGeometry,
    previousLinks: HTMLAnchorElement[],
    currentLinks: HTMLAnchorElement[],
    refreshIndicator: boolean
) {
    const changed = !linksAreEqual(previousLinks, currentLinks)
    if (!changed && !refreshIndicator) return previousLinks
    if (changed) {
        previousLinks.forEach((link) => {
            link.classList.remove(ACTIVE_CLASS)
            geometry.items.get(link)?.item.classList.remove(ACTIVE_CLASS)
        })
        currentLinks.forEach((link) => {
            link.classList.add(ACTIVE_CLASS)
            geometry.items.get(link)?.item.classList.add(ACTIVE_CLASS)
        })
    }
    updateIndicator(elements, geometry, currentLinks)
    return currentLinks
}

function setupSmoothScrolling(elements: TocElements, signal: AbortSignal) {
    elements.tocLinks.forEach((link) =>
        link.addEventListener(
            'click',
            (e) => {
                const href = link.getAttribute('href')
                if (href?.charAt(0) === '#') {
                    const target = document.getElementById(href.slice(1))
                    if (target) {
                        e.preventDefault()
                        target.scrollIntoView({ behavior: 'smooth' })
                        history.pushState(null, '', href)
                    }
                }
            },
            { signal }
        )
    )
}

export function initTocNav() {
    initializationController?.abort()
    initializationController = new window.AbortController()
    const { signal } = initializationController
    const elements = initializeTocElements()
    if (!elements.tocContainer || !elements.progressIndicator) return
    elements.progressIndicator.style.height = '0'
    elements.progressIndicator.style.top = '0'
    let geometry = cacheGeometry(elements)
    let currentLinks = elements.tocLinks.filter((link) =>
        link.classList.contains(ACTIVE_CLASS)
    )
    currentLinks.forEach((link) =>
        geometry.items.get(link)?.item.classList.add(ACTIVE_CLASS)
    )
    let animationFrame: number | null = null
    let refreshPending = false,
        updatePending = false
    const update = (refreshIndicator: boolean) => {
        const scrollY = window.scrollY
        const nextLinks =
            scrollY >= geometry.maximumScrollPosition - BOTTOM_TOLERANCE
                ? getBottomLinks(geometry, scrollY)
                : getCurrentLinks(geometry.headings, scrollY)
        currentLinks = updateActiveLinks(
            elements,
            geometry,
            currentLinks,
            nextLinks,
            refreshIndicator
        )
    }
    const runScheduledWork = () => {
        animationFrame = null
        const shouldRefresh = refreshPending,
            shouldUpdate = updatePending
        refreshPending = false
        updatePending = false
        if (shouldRefresh) geometry = cacheGeometry(elements)
        if (shouldUpdate) update(shouldRefresh)
    }
    const scheduleFrame = () => {
        if (animationFrame === null)
            animationFrame = window.requestAnimationFrame(runScheduledWork)
    }
    const scheduleUpdate = () => {
        updatePending = true
        scheduleFrame()
    }
    const scheduleRefresh = () => {
        refreshPending = true
        updatePending = true
        scheduleFrame()
    }
    update(true)
    window.addEventListener('scroll', scheduleUpdate, { passive: true, signal })
    window.addEventListener('resize', scheduleRefresh, {
        passive: true,
        signal,
    })
    document.addEventListener(
        'click',
        (event) => {
            if ((event.target as Element | null)?.closest('.tabs-label'))
                scheduleRefresh()
        },
        { signal }
    )
    document.addEventListener(
        'change',
        (event) => {
            if ((event.target as Element | null)?.matches('.tabs-input'))
                scheduleRefresh()
        },
        { signal }
    )
    document.addEventListener('toggle', scheduleRefresh, {
        capture: true,
        signal,
    })
    const resizeObserver = new ResizeObserver(scheduleRefresh)
    if (elements.contentRoot) resizeObserver.observe(elements.contentRoot)
    resizeObserver.observe(elements.tocContainer)
    signal.addEventListener('abort', () => {
        resizeObserver.disconnect()
        if (animationFrame !== null) window.cancelAnimationFrame(animationFrame)
    })
    setupSmoothScrolling(elements, signal)
}
