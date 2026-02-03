import {
    applyHtmxAttributes,
    getPathFromUrl,
    isExternalDocsUrl,
    useCurrentPathname,
} from './utils'
import htmx from 'htmx.org'
import { RefObject, useEffect, useMemo, useRef } from 'react'

export interface UseHtmxLinkResult {
    /** Ref to attach to the anchor element */
    ref: RefObject<HTMLAnchorElement>
    /** Normalized path for the href attribute (falls back to original URL if not an elastic.co docs link) */
    href: string
}

/**
 * Hook that applies htmx attributes to a single anchor element.
 * Returns a ref to attach to the anchor element and the normalized href.
 *
 * Handles both paths (/docs/...) and full URLs (https://elastic.co/docs/...).
 * HTMX attributes are not applied for:
 * - External non-elastic.co URLs
 * - External docs URLs (e.g., /docs/api) which are served from separate sites
 *
 * @param url - The path or full URL for the link
 * @returns Object with ref and normalized href
 *
 * @example
 * const { ref, href } = useHtmxLink('/docs/elasticsearch/reference')
 * return <a ref={ref} href={href}>Link</a>
 *
 * @example
 * // Also handles full URLs
 * const { ref, href } = useHtmxLink('https://elastic.co/docs/elasticsearch/reference')
 * // href will be '/docs/elasticsearch/reference'
 */
export const useHtmxLink = (url: string): UseHtmxLinkResult => {
    const anchorRef = useRef<HTMLAnchorElement>(null)
    const currentPathname = useCurrentPathname()

    // Normalize URL to path, returns null for external non-elastic.co URLs
    const path = useMemo(() => getPathFromUrl(url), [url])

    // Use normalized path for href, fall back to original URL for external links
    const href = path ?? url

    useEffect(() => {
        if (!anchorRef.current) return

        const isExternal = !path || isExternalDocsUrl(path)

        if (isExternal) {
            // Explicitly disable HTMX for external links
            anchorRef.current.setAttribute('hx-disable', 'true')
        } else {
            // Apply HTMX attributes for internal docs links
            applyHtmxAttributes(anchorRef.current, path, currentPathname)
            htmx.process(anchorRef.current)
        }
    }, [path, currentPathname])

    return { ref: anchorRef, href }
}
