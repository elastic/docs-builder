import { getPathFromUrl, isExternalDocsUrl } from './utils'
import htmx from 'htmx.org'
import { RefObject, useEffect } from 'react'

/**
 * Hook that processes all links in a container element.
 * For internal docs links, applies htmx attributes for SPA navigation.
 * For external links (non-elastic.co or /docs/api), adds hx-disable to prevent htmx processing.
 *
 * @param containerRef - Ref to the container element
 * @param dependencies - Additional dependencies that should trigger reprocessing (e.g., content)
 *
 * @example
 * const containerRef = useRef<HTMLDivElement>(null)
 * useHtmxContainer(containerRef, [htmlContent])
 * return <div ref={containerRef} dangerouslySetInnerHTML={{ __html: htmlContent }} />
 */
export const useHtmxContainer = (
    containerRef: RefObject<HTMLElement | null>,
    dependencies: unknown[] = []
): void => {
    useEffect(() => {
        if (!containerRef.current) return

        const links = containerRef.current.querySelectorAll('a[href]')
        let hasProcessedLinks = false

        links.forEach((link) => {
            const anchor = link as HTMLAnchorElement
            const href = anchor.getAttribute('href') || ''
            const path = getPathFromUrl(href)

            // External non-elastic.co URLs - disable htmx
            if (!path) {
                anchor.setAttribute('hx-disable', 'true')
                return
            }

            // External docs URLs (e.g., /docs/api) - disable htmx
            if (isExternalDocsUrl(path)) {
                anchor.setAttribute('href', path)
                anchor.setAttribute('hx-disable', 'true')
                return
            }

            // Internal docs links inherit hx-boost from <body>
            anchor.setAttribute('href', path)
            hasProcessedLinks = true
        })

        // Process the entire container with htmx if we modified any links
        if (hasProcessedLinks) {
            htmx.process(containerRef.current)
        }
    }, [...dependencies])
}
