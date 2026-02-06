import { initApiDocs } from './api-docs'
import { initAppliesSwitch } from './applies-switch'
import { initCopyButton } from './copybutton'
import { initHighlight } from './hljs'
import { initImageCarousel } from './image-carousel'
import { initIsolatedHeader, setInitialHeaderOffset } from './isolated-header'
import { initMermaid } from './mermaid'
import { openDetailsWithAnchor } from './open-details-with-anchor'
import { initNav } from './pages-nav'
import { initSmoothScroll } from './smooth-scroll'
import { initTabs } from './tabs'
import { initializeOtel } from './telemetry/instrumentation'
import { initTocNav } from './toc-nav'
import 'htmx-ext-head-support'
import 'htmx-ext-preload'
import * as katex from 'katex'
import { $, $$ } from 'select-dom'
import { UAParser } from 'ua-parser-js'

// Injected at build time from MinVer
const DOCS_BUILDER_VERSION =
    process.env.DOCS_BUILDER_VERSION?.trim() ?? '0.0.0-dev'

// Initialize OpenTelemetry FIRST, before any other code runs
// This must happen early so all subsequent code is instrumented
initializeOtel({
    serviceName: 'docs-frontend',
    serviceVersion: DOCS_BUILDER_VERSION,
    baseUrl: '/docs',
    debug: false,
})

// Set header offset immediately to prevent layout shift on reload
// This runs before DOMContentLoaded to avoid visual jump
setInitialHeaderOffset()

// Dynamically import web components after telemetry is initialized
// This ensures telemetry is available when the components execute
// Parcel will automatically code-split this into a separate chunk
import('./web-components/NavigationSearch/NavigationSearchComponent')
import('./web-components/AskAi/AskAi')
import('./web-components/VersionDropdown')
import('./web-components/AppliesToPopover')
import('./web-components/FullPageSearch/FullPageSearchComponent')
import('./web-components/Diagnostics/DiagnosticsComponent')
import('./web-components/Header/Header')

const { getOS } = new UAParser()

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type HtmxEvent = any

/**
 * Initialize KaTeX math rendering for elements with class 'math'
 */
function initMath() {
    const mathElements = $$('.math:not([data-katex-processed])')
    mathElements.forEach((element) => {
        try {
            const content = element.textContent?.trim()
            if (!content) return

            // Determine if this is display math based on content and element type
            const isDisplayMath =
                element.tagName === 'DIV' ||
                content.includes('\\[') ||
                content.includes('$$') ||
                content.includes('\\begin{') ||
                content.includes('\n')

            // Clean up common LaTeX delimiters
            const cleanContent = content
                .replace(/^\$\$|\$\$$/g, '') // Remove $$ delimiters
                .replace(/^\\\[|\\\]$/g, '') // Remove \[ \] delimiters
                .trim()

            // Clear the element content before rendering
            element.innerHTML = ''

            katex.render(cleanContent, element, {
                throwOnError: false,
                displayMode: isDisplayMath,
                strict: false, // Allow some LaTeX extensions
                trust: false, // Security: don't trust arbitrary commands
                output: 'mathml', // Only render MathML, not HTML
                macros: {
                    // Add common macros if needed
                },
            })

            // Mark as processed to prevent double processing
            element.setAttribute('data-katex-processed', 'true')
        } catch (error) {
            console.warn('KaTeX rendering error:', error)
            // Fallback: keep the original content
            element.innerHTML = element.textContent || ''
        }
    })
}

// Initialize on initial page load
document.addEventListener('DOMContentLoaded', function () {
    initMath()
    initMermaid()
    initIsolatedHeader()
    // #region agent log
    setTimeout(() => {
        const elasticDocsHeader = document.querySelector('elastic-docs-header')
        const headerHeight = elasticDocsHeader
            ? elasticDocsHeader.getBoundingClientRect().height
            : 0
        const headerRect = elasticDocsHeader
            ? elasticDocsHeader.getBoundingClientRect()
            : null
        const computedOffsetTop = getComputedStyle(
            document.documentElement
        ).getPropertyValue('--offset-top')
        const stickyNav = document.querySelector('.sidebar')
        const stickyNavRect = stickyNav
            ? (stickyNav as HTMLElement).getBoundingClientRect()
            : null
        const stickyNavTop = stickyNav
            ? getComputedStyle(stickyNav as HTMLElement).top
            : ''
        const mainContainer = document.getElementById('main-container')
        const mainContainerRect = mainContainer
            ? mainContainer.getBoundingClientRect()
            : null
        const bodyRect = document.body.getBoundingClientRect()
        const htmlHeight = getComputedStyle(document.documentElement).height
        const bodyHeight = getComputedStyle(document.body).height
        const bodyDisplay = getComputedStyle(document.body).display
        const bodyFlexDirection = getComputedStyle(document.body).flexDirection
        const mainContainerHeight = mainContainer
            ? getComputedStyle(mainContainer).height
            : ''
        const mainContainerFlex = mainContainer
            ? getComputedStyle(mainContainer).flex
            : ''
        const mainContainerParent = mainContainer?.parentElement
        const mainContainerParentDisplay = mainContainerParent
            ? getComputedStyle(mainContainerParent).display
            : ''
        const bodyChildren = Array.from(document.body.children).map(
            (child, i) => ({
                index: i,
                tagName: child.tagName,
                id: child.id,
                className: child.className,
            })
        )
        const footer = document.querySelector('footer')
        const footerRect = footer ? footer.getBoundingClientRect() : null
        fetch(
            'http://127.0.0.1:7242/ingest/9d6d8669-b090-4d8a-8c8e-a640fc9736de',
            {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    location: 'main.ts:102',
                    message: 'DOMContentLoaded POST-FIX structure analysis',
                    data: {
                        headerHeight,
                        headerTop: headerRect?.top,
                        computedOffsetTop,
                        stickyNavTop,
                        stickyNavActualTop: stickyNavRect?.top,
                        mainContainerTop: mainContainerRect?.top,
                        mainContainerHeight,
                        mainContainerRectHeight: mainContainerRect?.height,
                        mainContainerFlex,
                        mainContainerParentTag: mainContainerParent?.tagName,
                        mainContainerParentDisplay,
                        bodyHeight,
                        bodyDisplay,
                        bodyFlexDirection,
                        bodyChildren,
                        bodyRectHeight: bodyRect.height,
                        htmlHeight,
                        viewportHeight: window.innerHeight,
                        footerTop: footerRect?.top,
                        footerHeight: footerRect?.height,
                        scrollY: window.scrollY,
                        pathname: window.location.pathname,
                    },
                    timestamp: Date.now(),
                    sessionId: 'debug-session',
                    runId: 'post-fix',
                    hypothesisId: 'D',
                }),
            }
        ).catch(() => {})
    }, 100)
    // #endregion
})

document.addEventListener('htmx:load', function () {
    // #region agent log
    const elasticDocsHeader = document.querySelector('elastic-docs-header')
    const headerHeight = elasticDocsHeader
        ? elasticDocsHeader.getBoundingClientRect().height
        : 0
    const computedOffsetTop = getComputedStyle(
        document.documentElement
    ).getPropertyValue('--offset-top')
    const stickyNav = document.querySelector('.sidebar')
    const stickyNavRect = stickyNav
        ? (stickyNav as HTMLElement).getBoundingClientRect()
        : null
    const stickyNavTop = stickyNav
        ? getComputedStyle(stickyNav as HTMLElement).top
        : ''
    fetch('http://127.0.0.1:7242/ingest/9d6d8669-b090-4d8a-8c8e-a640fc9736de', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            location: 'main.ts:121',
            message: 'htmx:load entry - BEFORE init',
            data: {
                headerHeight,
                computedOffsetTop,
                stickyNavTop,
                stickyNavActualTop: stickyNavRect?.top,
                scrollY: window.scrollY,
                pathname: window.location.pathname,
            },
            timestamp: Date.now(),
            sessionId: 'debug-session',
            runId: 'run1',
            hypothesisId: 'A',
        }),
    }).catch(() => {})
    // #endregion
    initTocNav()
    initHighlight()
    initCopyButton()
    initTabs()
    initAppliesSwitch()
    initMath()
    initMermaid()
    initNav()

    initSmoothScroll()
    openDetailsWithAnchor()
    initImageCarousel()
    initApiDocs()
    // #region agent log
    setTimeout(() => {
        const elasticDocsHeaderAfter = document.querySelector(
            'elastic-docs-header'
        )
        const headerHeightAfter = elasticDocsHeaderAfter
            ? elasticDocsHeaderAfter.getBoundingClientRect().height
            : 0
        const headerRectAfter = elasticDocsHeaderAfter
            ? elasticDocsHeaderAfter.getBoundingClientRect()
            : null
        const computedOffsetTopAfter = getComputedStyle(
            document.documentElement
        ).getPropertyValue('--offset-top')
        const stickyNavAfter = document.querySelector('.sidebar')
        const stickyNavRectAfter = stickyNavAfter
            ? (stickyNavAfter as HTMLElement).getBoundingClientRect()
            : null
        const stickyNavTopAfter = stickyNavAfter
            ? getComputedStyle(stickyNavAfter as HTMLElement).top
            : ''
        const mainContainerAfter = document.getElementById('main-container')
        const mainContainerRectAfter = mainContainerAfter
            ? mainContainerAfter.getBoundingClientRect()
            : null
        const bodyHeightAfter = getComputedStyle(document.body).height
        const mainContainerHeightAfter = mainContainerAfter
            ? getComputedStyle(mainContainerAfter).height
            : ''
        const mainContainerFlexAfter = mainContainerAfter
            ? getComputedStyle(mainContainerAfter).flex
            : ''
        const footerAfter = document.querySelector('footer')
        const footerRectAfter = footerAfter
            ? footerAfter.getBoundingClientRect()
            : null
        fetch(
            'http://127.0.0.1:7242/ingest/9d6d8669-b090-4d8a-8c8e-a640fc9736de',
            {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    location: 'main.ts:135',
                    message: 'htmx:load POST-FIX flex layout',
                    data: {
                        headerHeightAfter,
                        headerTopAfter: headerRectAfter?.top,
                        computedOffsetTopAfter,
                        stickyNavTopAfter,
                        stickyNavActualTopAfter: stickyNavRectAfter?.top,
                        mainContainerTopAfter: mainContainerRectAfter?.top,
                        mainContainerHeightAfter,
                        mainContainerRectHeightAfter:
                            mainContainerRectAfter?.height,
                        mainContainerFlexAfter,
                        bodyHeightAfter,
                        footerTopAfter: footerRectAfter?.top,
                        footerHeightAfter: footerRectAfter?.height,
                        scrollY: window.scrollY,
                        pathname: window.location.pathname,
                    },
                    timestamp: Date.now(),
                    sessionId: 'debug-session',
                    runId: 'post-fix',
                    hypothesisId: 'D',
                }),
            }
        ).catch(() => {})
    }, 100)
    // #endregion

    const urlParams = new URLSearchParams(window.location.search)
    const editParam = urlParams.has('edit')
    if (editParam) {
        $('.edit-this-page.hidden')?.classList.remove('hidden')
    }
})

// Don't remove style tags because they are used by the elastic global nav.
document.addEventListener(
    'htmx:removingHeadElement',
    function (event: HtmxEvent) {
        const tagName = event.detail.headElement.tagName
        if (tagName === 'STYLE') {
            event.preventDefault()
        }
    }
)

document.addEventListener('htmx:beforeRequest', function (event: HtmxEvent) {
    const path = event.detail.requestConfig?.path

    // Bypass htmx for /api URLs - they require full page navigation
    if (path?.startsWith('/api')) {
        event.preventDefault()
        window.location.href = path
        return
    }

    if (
        event.detail.requestConfig.verb === 'get' &&
        event.detail.requestConfig.triggeringEvent
    ) {
        const { ctrlKey, metaKey, shiftKey }: PointerEvent =
            event.detail.requestConfig.triggeringEvent
        const { name: os } = getOS()
        const modifierKey: boolean = os === 'macOS' ? metaKey : ctrlKey
        if (shiftKey || modifierKey) {
            event.preventDefault()
            window.open(
                event.detail.requestConfig.path,
                '_blank',
                'noopener,noreferrer'
            )
        }
    }
})

document.body.addEventListener(
    'htmx:oobBeforeSwap',
    function (event: HtmxEvent) {
        // #region agent log
        const elasticDocsHeader = document.querySelector('elastic-docs-header')
        const headerHeight = elasticDocsHeader
            ? elasticDocsHeader.getBoundingClientRect().height
            : 0
        const computedOffsetTop = getComputedStyle(
            document.documentElement
        ).getPropertyValue('--offset-top')
        const stickyNav = document.querySelector('.sidebar')
        const stickyNavRect = stickyNav
            ? (stickyNav as HTMLElement).getBoundingClientRect()
            : null
        const stickyNavTop = stickyNav
            ? getComputedStyle(stickyNav as HTMLElement).top
            : ''
        fetch(
            'http://127.0.0.1:7242/ingest/9d6d8669-b090-4d8a-8c8e-a640fc9736de',
            {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    location: 'main.ts:183',
                    message: 'htmx:oobBeforeSwap',
                    data: {
                        targetId: event.target?.id,
                        headerHeight,
                        computedOffsetTop,
                        stickyNavTop,
                        stickyNavActualTop: stickyNavRect?.top,
                        scrollY: window.scrollY,
                        pathname: window.location.pathname,
                    },
                    timestamp: Date.now(),
                    sessionId: 'debug-session',
                    runId: 'run1',
                    hypothesisId: 'A',
                }),
            }
        ).catch(() => {})
        // #endregion
        // This is needed to scroll to the top of the page when the content is swapped
        if (
            event.target?.id === 'main-container' ||
            event.target?.id === 'markdown-content' ||
            event.target?.id === 'content-container'
        ) {
            window.scrollTo(0, 0)
            // #region agent log
            setTimeout(() => {
                const computedOffsetTopAfter = getComputedStyle(
                    document.documentElement
                ).getPropertyValue('--offset-top')
                const stickyNavAfter = document.querySelector('.sidebar')
                const stickyNavRectAfter = stickyNavAfter
                    ? (stickyNavAfter as HTMLElement).getBoundingClientRect()
                    : null
                const stickyNavTopAfter = stickyNavAfter
                    ? getComputedStyle(stickyNavAfter as HTMLElement).top
                    : ''
                fetch(
                    'http://127.0.0.1:7242/ingest/9d6d8669-b090-4d8a-8c8e-a640fc9736de',
                    {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            location: 'main.ts:195',
                            message: 'After scrollTo(0,0)',
                            data: {
                                computedOffsetTopAfter,
                                stickyNavTopAfter,
                                stickyNavActualTopAfter:
                                    stickyNavRectAfter?.top,
                                scrollY: window.scrollY,
                            },
                            timestamp: Date.now(),
                            sessionId: 'debug-session',
                            runId: 'run1',
                            hypothesisId: 'A',
                        }),
                    }
                ).catch(() => {})
            }, 50)
            // #endregion
        }
    }
)

document.body.addEventListener(
    'htmx:responseError',
    function (event: HtmxEvent) {
        // If you get a 404 error while clicking on a hx-get link, actually open the link
        // This is needed because the browser doesn't update the URL when the response is a 404
        // In production, cloudfront handles serving the 404 page.
        // Locally, the DocumentationWebHost handles it.
        // On previews, a generic 404 page is shown.
        if (event.detail.xhr.status === 404) {
            window.location.assign(event.detail.pathInfo.requestPath)
        }
    }
)

// We add a query string to the get request to make sure the requested page is up to date
const docsBuilderVersion = $('body')?.dataset.docsBuilderVersion
document.body.addEventListener(
    'htmx:configRequest',
    function (event: HtmxEvent) {
        if (event.detail.verb === 'get' && docsBuilderVersion) {
            event.detail.parameters['v'] = docsBuilderVersion
        }
    }
)

// Here we need to strip the v parameter from the URL so
// that the browser doesn't show the v parameter in the address bar
document.body.addEventListener(
    'htmx:beforeHistoryUpdate',
    function (event: HtmxEvent) {
        const params = new URLSearchParams(
            event.detail.history.path.split('?')[1] ?? ''
        )
        params.delete('v')
        const pathWithoutQueryString = event.detail.history.path.split('?')[0]
        if (params.size === 0) {
            event.detail.history.path = pathWithoutQueryString
        } else {
            event.detail.history.path =
                pathWithoutQueryString + '?' + params.toString()
        }
    }
)
