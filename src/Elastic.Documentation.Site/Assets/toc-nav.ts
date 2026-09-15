import { $$optional, $optional } from 'select-dom'

interface HeadingEntry {
    heading: Element
    link: HTMLAnchorElement | null
}

interface TocElements {
    headings: HeadingEntry[]
    tocLinks: HTMLAnchorElement[]
    tocContainer: HTMLDivElement | null
    progressIndicator: HTMLDivElement | null
}

// 34 is the height of the header + some padding
// 4 is the base spacing unit
const HEADING_OFFSET = 34 * 4
const VISIBLE_HEADING_OFFSET = HEADING_OFFSET - 64
const ACTIVE_CLASS = 'toc-current'

let initializationController: AbortController | null = null

function initializeTocElements(): TocElements {
    // Support both regular docs (#markdown-content) and API docs (#elastic-api-v3)
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
        const link = anchorId ? linksByAnchor.get(`#${anchorId}`) : null
        return {
            heading,
            link: link ?? null,
        }
    })
    return { headings, tocLinks, tocContainer, progressIndicator }
}

// Get the anchor ID for a heading element
// Supports regular docs, directive headings with their own id, and API docs
function getHeadingAnchorId(heading: Element): string | null {
    // For API docs: h3[data-section]
    const dataSection = heading.getAttribute('data-section')
    if (dataSection) {
        return dataSection
    }
    // For regular docs: .heading-wrapper parent with id
    const wrapper = heading.closest('.heading-wrapper')
    return wrapper?.id || heading.id || null
}

function findCurrentHeadingIndex(
    headings: HeadingEntry[],
    currentIndex: number
): number {
    while (
        currentIndex >= 0 &&
        headings[currentIndex].heading.getBoundingClientRect().top >
            HEADING_OFFSET
    ) {
        currentIndex--
    }

    while (
        currentIndex + 1 < headings.length &&
        headings[currentIndex + 1].heading.getBoundingClientRect().top <=
            HEADING_OFFSET
    ) {
        currentIndex++
    }
    return currentIndex
}

function getCurrentLinks(
    headings: HeadingEntry[],
    currentIndex: number
): HTMLAnchorElement[] {
    if (currentIndex < 0) return []

    const currentTop =
        headings[currentIndex].heading.getBoundingClientRect().top
    let firstIndex = currentIndex
    while (firstIndex > 0) {
        const previousTop =
            headings[firstIndex - 1].heading.getBoundingClientRect().top
        if (Math.abs(previousTop - currentTop) > 1) break
        firstIndex--
    }
    return headings
        .slice(firstIndex, currentIndex + 1)
        .map(({ link }) => link)
        .filter((link): link is HTMLAnchorElement => link !== null)
}

function getBottomLinks(
    headings: HeadingEntry[],
    currentIndex: number
): HTMLAnchorElement[] {
    let firstIndex = Math.max(currentIndex, 0)
    while (
        firstIndex > 0 &&
        headings[firstIndex - 1].heading.getBoundingClientRect().top >=
            VISIBLE_HEADING_OFFSET
    ) {
        firstIndex--
    }

    let lastIndex = firstIndex
    while (
        lastIndex + 1 < headings.length &&
        headings[lastIndex + 1].heading.getBoundingClientRect().top <=
            window.innerHeight
    ) {
        lastIndex++
    }

    return headings
        .slice(firstIndex, lastIndex + 1)
        .filter(({ heading }) => {
            const top = heading.getBoundingClientRect().top
            return top >= VISIBLE_HEADING_OFFSET && top <= window.innerHeight
        })
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

function updateActiveLinks(
    elements: TocElements,
    previousLinks: HTMLAnchorElement[],
    currentLinks: HTMLAnchorElement[]
) {
    if (linksAreEqual(previousLinks, currentLinks)) return previousLinks

    const linkElements = currentLinks
        .map((link) => link.closest('li'))
        .filter((item): item is HTMLLIElement => item !== null)
    const firstLinkRect = linkElements[0]?.getBoundingClientRect()
    const lastLinkRect =
        linkElements[linkElements.length - 1]?.getBoundingClientRect()
    const tocRect = elements.tocContainer?.getBoundingClientRect()

    previousLinks.forEach((link) => link.classList.remove(ACTIVE_CLASS))
    currentLinks.forEach((link) => link.classList.add(ACTIVE_CLASS))

    if (
        elements.progressIndicator &&
        tocRect &&
        firstLinkRect &&
        lastLinkRect
    ) {
        const top = firstLinkRect.top - tocRect.top
        const height =
            lastLinkRect.top + lastLinkRect.height - firstLinkRect.top
        const transform = `translateY(${top}px)`
        const heightValue = `${height}px`
        if (elements.progressIndicator.style.transform !== transform)
            elements.progressIndicator.style.transform = transform
        if (elements.progressIndicator.style.height !== heightValue)
            elements.progressIndicator.style.height = heightValue
    }
    return currentLinks
}

function setupSmoothScrolling(elements: TocElements, signal: AbortSignal) {
    elements.tocLinks.forEach((link) => {
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
    })
}

export function initTocNav() {
    initializationController?.abort()
    initializationController = new window.AbortController()
    const { signal } = initializationController
    const elements = initializeTocElements()
    if (!elements.tocContainer || !elements.progressIndicator) return

    elements.progressIndicator.style.height = '0'
    elements.progressIndicator.style.top = '0'

    let currentHeadingIndex = -1
    let currentLinks = elements.tocLinks.filter((link) =>
        link.classList.contains(ACTIVE_CLASS)
    )
    let animationFrame: number | null = null
    const update = () => {
        currentHeadingIndex = findCurrentHeadingIndex(
            elements.headings,
            currentHeadingIndex
        )
        const isAtBottom =
            window.innerHeight + window.scrollY >=
            document.documentElement.scrollHeight - 10
        const nextLinks = isAtBottom
            ? getBottomLinks(elements.headings, currentHeadingIndex)
            : getCurrentLinks(elements.headings, currentHeadingIndex)
        currentLinks = updateActiveLinks(elements, currentLinks, nextLinks)
    }
    const scheduleUpdate = () => {
        if (animationFrame !== null) return
        animationFrame = window.requestAnimationFrame(() => {
            animationFrame = null
            update()
        })
    }

    update()
    window.addEventListener('scroll', scheduleUpdate, {
        passive: true,
        signal,
    })
    window.addEventListener('resize', scheduleUpdate, {
        passive: true,
        signal,
    })
    signal.addEventListener('abort', () => {
        if (animationFrame !== null) window.cancelAnimationFrame(animationFrame)
    })
    setupSmoothScrolling(elements, signal)
}
