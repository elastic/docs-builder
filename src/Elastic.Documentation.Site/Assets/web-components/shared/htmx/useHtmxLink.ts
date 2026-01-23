import { applyHtmxAttributes, useCurrentPathname } from './utils'
import htmx from 'htmx.org'
import { RefObject, useEffect, useRef } from 'react'

/**
 * Hook that applies htmx attributes to a single anchor element.
 * Returns a ref to attach to the anchor element.
 *
 * @param path - The path/href for the link
 * @returns A ref to attach to the anchor element
 *
 * @example
 * const anchorRef = useHtmxLink('/docs/elasticsearch/reference')
 * return <a ref={anchorRef} href="/docs/elasticsearch/reference">Link</a>
 */
export const useHtmxLink = (path: string): RefObject<HTMLAnchorElement> => {
    const anchorRef = useRef<HTMLAnchorElement>(null)
    const currentPathname = useCurrentPathname()

    useEffect(() => {
        if (anchorRef.current) {
            applyHtmxAttributes(anchorRef.current, path, currentPathname)
            htmx.process(anchorRef.current)
        }
    }, [path, currentPathname])

    return anchorRef
}
