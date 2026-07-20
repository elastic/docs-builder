import { getPathFromUrl, isExternalDocsUrl } from './utils'
import htmx from 'htmx.org'
import { RefObject, useEffect } from 'react'

/**
 * Hook that normalizes all links in a container element: internal docs links
 * get path hrefs and inherit hx-boost from <body>; external links (other
 * domains, /docs/api) get hx-disable so htmx leaves them alone.
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
    }, dependencies)
}
