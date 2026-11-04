import {
    getPathFromUrl,
    isExternalDocsUrl,
} from '../web-components/shared/htmx/utils'
import { UAParser } from 'ua-parser-js'

const { getOS } = new UAParser()

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type HtmxEvent = any

type HtmxHookOptions = {
    // A bundle can ask for a full page load on top of the external-URL rule, for example when
    // the swap would leave stale page chrome in place.
    forcesFullPageLoad?: (targetPath: string) => boolean
}

export function bindHtmxHooks({ forcesFullPageLoad }: HtmxHookOptions = {}) {
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

    document.addEventListener(
        'htmx:beforeRequest',
        function (event: HtmxEvent) {
            if (event.detail.requestConfig.verb !== 'get') return
            // Speculative prefetches from the preload extension must pass through
            // untouched — without this, preloading a non-docs link would trigger the
            // full-page-load fallback below on mere mousedown/hover.
            if (event.detail.requestConfig.headers['HX-Preloaded'] === 'true')
                return
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
            const targetPath = new URL(path, location.href).pathname
            const docsPath = getPathFromUrl(targetPath)
            if (
                !docsPath ||
                isExternalDocsUrl(docsPath) ||
                forcesFullPageLoad?.(targetPath)
            ) {
                event.preventDefault()
                window.location.assign(path)
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
}
