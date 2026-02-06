/**
 * Handles the dynamic resizing of the isolated header when scrolling.
 * Expands only at scroll position 0, compacts after scrolling past threshold.
 */

const COMPACT_THRESHOLD = 80 // Scroll down past this to compact
const EXPANDED_OFFSET = '110px'
const COMPACT_OFFSET = '48px'

/**
 * Set the CSS variable immediately based on scroll position.
 * Called early (before DOMContentLoaded) to prevent layout shift.
 * Adds no-transitions class that will be removed by initIsolatedHeader.
 */
export function setInitialHeaderOffset() {
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/9d6d8669-b090-4d8a-8c8e-a640fc9736de', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            location: 'isolated-header.ts:15',
            message: 'setInitialHeaderOffset entry',
            data: {
                hasIsolatedHeaderId:
                    !!document.getElementById('isolated-header'),
                hasElasticDocsHeader: !!document.querySelector(
                    'elastic-docs-header'
                ),
                scrollY: window.scrollY,
            },
            timestamp: Date.now(),
            sessionId: 'debug-session',
            runId: 'run1',
            hypothesisId: 'A',
        }),
    }).catch(() => {})
    // #endregion
    // Check if elastic-docs-header exists (new structure) or isolated-header (old structure)
    const elasticDocsHeader = document.querySelector('elastic-docs-header')
    const isolatedHeader = document.getElementById('isolated-header')

    if (!elasticDocsHeader && !isolatedHeader) {
        // #region agent log
        fetch(
            'http://127.0.0.1:7242/ingest/9d6d8669-b090-4d8a-8c8e-a640fc9736de',
            {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    location: 'isolated-header.ts:21',
                    message: 'Early return - no header found',
                    data: {
                        computedOffsetTop: getComputedStyle(
                            document.documentElement
                        ).getPropertyValue('--offset-top'),
                    },
                    timestamp: Date.now(),
                    sessionId: 'debug-session',
                    runId: 'run1',
                    hypothesisId: 'A',
                }),
            }
        ).catch(() => {})
        // #endregion
        return
    }

    // Add class to disable all transitions during initial load
    // This will be removed by initIsolatedHeader after layout is set
    document.documentElement.classList.add('no-transitions')

    // For elastic-docs-header, CSS handles the offset via :has() selector
    // For isolated-header, use compact/expanded logic
    if (elasticDocsHeader) {
        // CSS will set --offset-top via :has(elastic-docs-header) selector
        // #region agent log
        fetch(
            'http://127.0.0.1:7242/ingest/9d6d8669-b090-4d8a-8c8e-a640fc9736de',
            {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    location: 'isolated-header.ts:34',
                    message:
                        'elastic-docs-header found, CSS will handle offset',
                    data: {
                        computedOffsetTop: getComputedStyle(
                            document.documentElement
                        ).getPropertyValue('--offset-top'),
                    },
                    timestamp: Date.now(),
                    sessionId: 'debug-session',
                    runId: 'run1',
                    hypothesisId: 'A',
                }),
            }
        ).catch(() => {})
        // #endregion
    } else if (isolatedHeader) {
        const offset = window.scrollY > 0 ? COMPACT_OFFSET : EXPANDED_OFFSET
        document.documentElement.style.setProperty('--offset-top', offset)
        // #region agent log
        fetch(
            'http://127.0.0.1:7242/ingest/9d6d8669-b090-4d8a-8c8e-a640fc9736de',
            {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    location: 'isolated-header.ts:38',
                    message: 'Set offset-top CSS variable (isolated-header)',
                    data: {
                        offset,
                        computedOffsetTop: getComputedStyle(
                            document.documentElement
                        ).getPropertyValue('--offset-top'),
                    },
                    timestamp: Date.now(),
                    sessionId: 'debug-session',
                    runId: 'run1',
                    hypothesisId: 'A',
                }),
            }
        ).catch(() => {})
        // #endregion
    }
}

/**
 * Full initialization - attaches scroll listener and updates header elements.
 * Should be called on DOMContentLoaded.
 */
export function initIsolatedHeader() {
    // #region agent log
    const elasticDocsHeader = document.querySelector('elastic-docs-header')
    const headerHeight = elasticDocsHeader
        ? elasticDocsHeader.getBoundingClientRect().height
        : 0
    const computedOffsetTop = getComputedStyle(
        document.documentElement
    ).getPropertyValue('--offset-top')
    const stickyNav = document.querySelector('.sidebar')
    const stickyNavTop = stickyNav
        ? getComputedStyle(stickyNav as HTMLElement).top
        : ''
    fetch('http://127.0.0.1:7242/ingest/9d6d8669-b090-4d8a-8c8e-a640fc9736de', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            location: 'isolated-header.ts:52',
            message: 'initIsolatedHeader entry',
            data: {
                hasIsolatedHeaderId:
                    !!document.getElementById('isolated-header'),
                hasElasticDocsHeader: !!elasticDocsHeader,
                headerHeight,
                computedOffsetTop,
                stickyNavTop,
                viewportHeight: window.innerHeight,
            },
            timestamp: Date.now(),
            sessionId: 'debug-session',
            runId: 'run1',
            hypothesisId: 'A',
        }),
    }).catch(() => {})
    // #endregion

    const elasticDocsHeaderEl = document.querySelector('elastic-docs-header')
    const isolatedHeaderEl = document.getElementById('isolated-header')

    // Handle elastic-docs-header (new structure) - CSS handles offset via :has() selector
    if (elasticDocsHeaderEl) {
        // Add class to body for CSS scoping
        document.body.classList.add('has-isolated-header')

        // CSS will set --offset-top via :has(elastic-docs-header) selector
        // #region agent log
        const stickyNavAfter = document.querySelector('.sidebar')
        const stickyNavTopAfter = stickyNavAfter
            ? getComputedStyle(stickyNavAfter as HTMLElement).top
            : ''
        fetch(
            'http://127.0.0.1:7242/ingest/9d6d8669-b090-4d8a-8c8e-a640fc9736de',
            {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    location: 'isolated-header.ts:66',
                    message:
                        'elastic-docs-header found, CSS will handle offset',
                    data: {
                        computedOffsetTop: getComputedStyle(
                            document.documentElement
                        ).getPropertyValue('--offset-top'),
                        stickyNavTopAfter,
                    },
                    timestamp: Date.now(),
                    sessionId: 'debug-session',
                    runId: 'run1',
                    hypothesisId: 'A',
                }),
            }
        ).catch(() => {})
        // #endregion

        // Re-enable transitions after layout is set
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                document.documentElement.classList.remove('no-transitions')
            })
        })

        return
    }

    // Handle isolated-header (old structure) - keep existing logic
    if (!isolatedHeaderEl) {
        // #region agent log
        fetch(
            'http://127.0.0.1:7242/ingest/9d6d8669-b090-4d8a-8c8e-a640fc9736de',
            {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    location: 'isolated-header.ts:85',
                    message:
                        'Early return - no header found in initIsolatedHeader',
                    data: {
                        computedOffsetTop: getComputedStyle(
                            document.documentElement
                        ).getPropertyValue('--offset-top'),
                    },
                    timestamp: Date.now(),
                    sessionId: 'debug-session',
                    runId: 'run1',
                    hypothesisId: 'A',
                }),
            }
        ).catch(() => {})
        // #endregion
        return
    }

    // Add class to body for CSS scoping
    document.body.classList.add('has-isolated-header')

    let isCompact = window.scrollY > 0

    const updateLayout = (compact: boolean) => {
        const offset = compact ? COMPACT_OFFSET : EXPANDED_OFFSET

        // Update CSS variable - this drives header height and sidebar positioning via CSS
        document.documentElement.style.setProperty('--offset-top', offset)
        // #region agent log
        const stickyNav = document.querySelector('.sidebar')
        const stickyNavTop = stickyNav
            ? getComputedStyle(stickyNav as HTMLElement).top
            : ''
        fetch(
            'http://127.0.0.1:7242/ingest/9d6d8669-b090-4d8a-8c8e-a640fc9736de',
            {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    location: 'isolated-header.ts:95',
                    message: 'updateLayout called',
                    data: {
                        compact,
                        offset,
                        computedOffsetTop: getComputedStyle(
                            document.documentElement
                        ).getPropertyValue('--offset-top'),
                        stickyNavTop,
                    },
                    timestamp: Date.now(),
                    sessionId: 'debug-session',
                    runId: 'run1',
                    hypothesisId: 'A',
                }),
            }
        ).catch(() => {})
        // #endregion

        // Update header internal elements
        isolatedHeaderEl
            .querySelectorAll<HTMLImageElement>('img')
            .forEach((img) => {
                img.style.width = compact ? '22px' : '28px'
                img.style.height = compact ? '22px' : '28px'
            })

        isolatedHeaderEl
            .querySelectorAll<HTMLElement>('.text-lg')
            .forEach((el) => {
                el.style.fontSize = compact ? '0.95rem' : ''
            })
    }

    const onScroll = () => {
        const scrollY = window.scrollY

        // Only expand when at the very top (scrollY === 0)
        // Only compact when scrolled past threshold
        if (!isCompact && scrollY > COMPACT_THRESHOLD) {
            isCompact = true
            updateLayout(true)
        } else if (isCompact && scrollY === 0) {
            isCompact = false
            updateLayout(false)
        }
    }

    window.addEventListener('scroll', onScroll, { passive: true })

    // Set initial state based on current scroll position
    updateLayout(isCompact)

    // Re-enable transitions after layout is set
    requestAnimationFrame(() => {
        requestAnimationFrame(() => {
            document.documentElement.classList.remove('no-transitions')
        })
    })
}
