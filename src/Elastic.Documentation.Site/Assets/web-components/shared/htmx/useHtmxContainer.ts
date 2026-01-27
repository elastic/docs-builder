import {
    applyHtmxAttributes,
    getPathFromUrl,
    useCurrentPathname,
} from './utils'
import htmx from 'htmx.org'
import { RefObject, useEffect } from 'react'

/**
 * Hook that processes all internal docs links in a container element.
 * Finds anchor elements, extracts paths, applies htmx attributes, and processes with htmx.
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
    const currentPathname = useCurrentPathname()

    useEffect(() => {
        if (!containerRef.current) return

        const links = containerRef.current.querySelectorAll('a[href]')
        let hasProcessedLinks = false

        links.forEach((link) => {
            const anchor = link as HTMLAnchorElement
            const href = anchor.getAttribute('href') || ''
            const path = getPathFromUrl(href)

            if (!path) return

            // Update href to use the path
            anchor.setAttribute('href', path)

            // Apply htmx attributes for in-page navigation
            applyHtmxAttributes(anchor, path, currentPathname)
            hasProcessedLinks = true
        })

        // Process the entire container with htmx if we modified any links
        if (hasProcessedLinks) {
            htmx.process(containerRef.current)
        }
    }, [currentPathname, ...dependencies])
}
