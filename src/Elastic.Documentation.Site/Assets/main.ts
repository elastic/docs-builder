import { initAgentSkillCopy } from './agent-skill'
import { initApiDocs } from './api-docs'
import { initAppliesSwitch } from './applies-switch'
import { config } from './config'
import { initCopyButton } from './copybutton'
import { initHighlight } from './hljs'
import { initImageCarousel } from './image-carousel'
import { initMermaid } from './mermaid'
import { openDetailsWithAnchor } from './open-details-with-anchor'
import { initNav } from './pages-nav'
import { initSmoothScroll } from './smooth-scroll'
import { initTable } from './table'
import { initTabs } from './tabs'
import { initializeOtel } from './telemetry/instrumentation'
import { logError, logInfo } from './telemetry/logging'
import {
    ATTR_CTA_NAME,
    ATTR_CTA_URL,
    ATTR_CTA_LABEL,
    ATTR_CTA_LOCATION,
    ATTR_EVENT_NAME,
    ATTR_URL_PATH,
    ATTR_URL_FULL,
} from './telemetry/semconv'
import { initTocNav } from './toc-nav'
import {
    getPathFromUrl,
    isExternalDocsUrl,
} from './web-components/shared/htmx/utils'
import 'htmx-ext-head-support'
import 'htmx-ext-preload'
import { $, $optional, $$optional } from 'select-dom'
import { UAParser } from 'ua-parser-js'

// Injected at build time from MinVer
const DOCS_BUILDER_VERSION =
    process.env.DOCS_BUILDER_VERSION?.trim() ?? '0.0.0-dev'

// Initialize OpenTelemetry FIRST, before any other code runs (when enabled)
// This must happen early so all subsequent code is instrumented
if (config.telemetryEnabled) {
    initializeOtel({
        serviceName: config.serviceName,
        serviceVersion: DOCS_BUILDER_VERSION,
        baseUrl: config.rootPath,
        debug: false,
    })
}

// Dynamically import web components after telemetry is initialized.
// Parcel code-splits these into separate chunks loaded on demand.
import('./web-components/VersionDropdown')
import('./web-components/AppliesToPopover')
import('./web-components/Diagnostics/DiagnosticsComponent')
import('./web-components/StorybookStory/StorybookStoryComponent')

if (config.buildType === 'isolated' || config.airGapped) {
    import('./isolated')
} else if (config.buildType === 'codex') {
    import('./codex')
}

const { getOS } = new UAParser()

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type HtmxEvent = any

// Run each init step in isolation so a failure in one does not abort the rest.
async function runInitSteps(
    steps: Array<[string, () => void | Promise<void>]>
) {
    for (const [name, init] of steps) {
        try {
            await init()
        } catch (error) {
            console.error(`Init step "${name}" failed:`, error)
            logError(`Init step failed: ${name}`, {
                'init.step': name,
                'error.message':
                    error instanceof Error ? error.message : String(error),
            })
        }
    }
}

function applyEditParam() {
    const urlParams = new URLSearchParams(window.location.search)
    if (urlParams.has('edit')) {
        $optional('.edit-this-page.hidden')?.classList.remove('hidden')
    }
}

/**
 * Initialize KaTeX math rendering for elements with class 'math'.
 * KaTeX's JS and fonts/CSS are lazy-loaded here so pages without math pay nothing for them.
 */
async function initMath() {
    const mathElements = $$optional('.math:not([data-katex-processed])')
    if (mathElements.length === 0) return

    const [katex] = await Promise.all([import('katex'), import('./katex.css')])

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

// Attributes shared by cta_viewed and cta_clicked so the two are directly comparable (CTR).
function ctaAttributes(cta: HTMLAnchorElement) {
    return {
        [ATTR_CTA_NAME]: cta.dataset.cta ?? '',
        [ATTR_CTA_URL]: cta.dataset.ctaUrl ?? '',
        [ATTR_CTA_LABEL]: cta.dataset.ctaLabel ?? '',
        [ATTR_CTA_LOCATION]: cta.dataset.ctaLocation ?? '',
        [ATTR_URL_PATH]: window.location.pathname,
        [ATTR_URL_FULL]: window.location.href,
    }
}

// Emit a CTA event: event.name mirrors the body so records can be filtered by
// exact keyword (attributes.event.name) instead of a text match on body.
function logCtaEvent(eventName: string, cta: HTMLAnchorElement) {
    logInfo(eventName, {
        [ATTR_EVENT_NAME]: eventName,
        ...ctaAttributes(cta),
    })
}

let ctaImpressionObserver: IntersectionObserver | null = null

// Fires 'cta_viewed' the first time a CTA becomes at least half visible, then stops
// observing it (one impression per page view). The observer is created once and
// reused across htmx swaps; re-observing an already-observed element is a no-op per
// spec, so this only needs to (re-)register CTAs that are new since the last swap.
function initCtaImpressions() {
    ctaImpressionObserver ??= new IntersectionObserver(
        (entries, observer) => {
            for (const entry of entries) {
                if (!entry.isIntersecting) continue
                logCtaEvent('cta_viewed', entry.target as HTMLAnchorElement)
                observer.unobserve(entry.target)
            }
        },
        { threshold: 0.5 }
    )
    $$optional('a[data-cta]').forEach((cta) =>
        ctaImpressionObserver?.observe(cta)
    )
}

// Initialize on initial page load
document.addEventListener('DOMContentLoaded', function () {
    runInitSteps([
        ['initMath', initMath],
        ['initMermaid', initMermaid],
        ['initCtaImpressions', initCtaImpressions],
    ])
})

document.addEventListener('htmx:load', function () {
    runInitSteps([
        ['initTocNav', initTocNav],
        ['initHighlight', initHighlight],
        ['initCopyButton', initCopyButton],
        ['initAgentSkillCopy', initAgentSkillCopy],
        ['initTabs', initTabs],
        ['initAppliesSwitch', initAppliesSwitch],
        ['initMath', initMath],
        ['initMermaid', initMermaid],
        ['initNav', initNav],
        ['initSmoothScroll', initSmoothScroll],
        ['openDetailsWithAnchor', openDetailsWithAnchor],
        ['initImageCarousel', initImageCarousel],
        ['initTable', initTable],
        ['initApiDocs', initApiDocs],
        ['applyEditParam', applyEditParam],
        ['initCtaImpressions', initCtaImpressions],
    ])
})

// Delegated listeners: survive htmx swaps without needing re-init, unlike the
// runInitSteps above which bind directly to elements that get replaced.
// logInfo's export survives this same-tab navigation: the batch processor flushes on
// 'pagehide' (registered in initializeOtel) via a keepalive fetch, which browsers keep
// alive past unload.
function handleCtaActivation(event: MouseEvent) {
    const cta = (event.target as HTMLElement)?.closest<HTMLAnchorElement>(
        'a[data-cta]'
    )
    if (!cta) return
    logCtaEvent('cta_clicked', cta)
}
document.addEventListener('click', handleCtaActivation)
// 'auxclick' with button 1 covers middle-click (open in new tab), which does NOT
// fire 'click' per the DOM spec - without this those opens went untracked. Button 2
// (right-click / context menu) also fires auxclick but isn't a real engagement.
document.addEventListener('auxclick', function (event: MouseEvent) {
    if (event.button === 1) handleCtaActivation(event)
})

// Don't remove style tags because they are used by the elastic global nav.
document.addEventListener(
    'htmx:removingHeadElement',
    function (event: HtmxEvent) {
        const headElement = event.detail.headElement
        if (headElement.tagName === 'STYLE') {
            event.preventDefault()
            return
        }
        // Keep the Storybook bootstrap assets that <storybook-story> injects into
        // <head>; htmx would otherwise strip them on navigation, which both breaks an
        // in-flight first load (the stylesheet's onload never fires) and leaves the
        // module-level load caches pointing at elements that no longer exist.
        if (
            headElement.dataset?.storybookScript !== undefined ||
            headElement.dataset?.storybookStyle !== undefined
        ) {
            event.preventDefault()
        }
    }
)

document.addEventListener('htmx:beforeRequest', function (event: HtmxEvent) {
    if (event.detail.requestConfig.verb !== 'get') return
    // Speculative prefetches from the preload extension must pass through
    // untouched — without this, preloading a non-docs link would trigger the
    // full-page-load fallback below on mere mousedown/hover.
    if (event.detail.requestConfig.headers['HX-Preloaded'] === 'true') return
    // Only boosted link navigation needs scoping; explicit hx-get widgets
    // manage their own requests.
    if (!event.detail.boosted) return
    const path: string = event.detail.requestConfig.path
    if (event.detail.requestConfig.triggeringEvent) {
        const { ctrlKey, metaKey, shiftKey }: PointerEvent =
            event.detail.requestConfig.triggeringEvent
        const { name: os } = getOS()
        const modifierKey: boolean = os === 'macOS' ? metaKey : ctrlKey
        if (shiftKey || modifierKey) {
            event.preventDefault()
            window.open(path, '_blank', 'noopener,noreferrer')
            return
        }
    }
    // hx-boost intercepts every same-origin link, but only internal docs URLs should
    // navigate through htmx (for assembler that means /docs/*; isolated and codex own
    // their whole origin). Anything else — marketing pages on the same domain, the
    // separate /docs/api app — gets a normal full page load.
    const docsPath = getPathFromUrl(new URL(path, location.href).pathname)
    if (!docsPath || isExternalDocsUrl(docsPath)) {
        event.preventDefault()
        window.location.assign(path)
    }
})

// Boosted navigations swap the whole <body>; scroll to top like a normal page load
document.body.addEventListener('htmx:afterSwap', function (event: HtmxEvent) {
    if (event.target === document.body) {
        window.scrollTo(0, 0)
    }
})

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
const docsBuilderVersion = $('body').dataset.docsBuilderVersion
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
