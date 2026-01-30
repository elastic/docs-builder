import { useEffect, useState } from 'react'

/**
 * Extracts the pathname from a URL string.
 * Handles both full URLs (https://...) and relative paths (/docs/...).
 * For full URLs, only returns the pathname if it's an elastic.co docs link.
 * Returns null for external non-docs links.
 */
export const getPathFromUrl = (url: string): string | null => {
    try {
        // Already a path - return as-is
        if (url.startsWith('/')) {
            return url
        }
        // Parse as full URL
        const parsed = new URL(url)
        // Only process elastic.co docs links
        if (
            parsed.hostname.includes('elastic.co') &&
            parsed.pathname.startsWith('/docs')
        ) {
            return parsed.pathname
        }
        return null
    } catch {
        return null
    }
}

/**
 * Gets the first segment from a URL path after removing /docs/ prefix.
 */
const getFirstSegment = (path: string): string =>
    path.replace('/docs/', '/').split('/')[1] ?? ''

/**
 * Checks if a path requires a simplified htmx swap (only #main-container).
 * Returns true for:
 * - Exactly "/docs" or "/docs/"
 * - Any path under "/docs/api/"
 */
const isSimpleSwapPath = (path: string): boolean => {
    // Normalize path - remove trailing slash for comparison
    const normalizedPath = path.endsWith('/') ? path.slice(0, -1) : path

    // Exactly /docs
    if (normalizedPath === '/docs') {
        return true
    }

    // Any path under /docs/api/
    if (path.startsWith('/docs/api/') || path === '/docs/api') {
        return true
    }

    return false
}

/**
 * Returns the appropriate hx-select-oob value based on whether
 * the target URL is in the same top-level group as the current URL.
 */
const getHxSelectOob = (targetUrl: string, currentPathname: string): string => {
    const currentSegment = getFirstSegment(currentPathname)
    const targetSegment = getFirstSegment(targetUrl)
    return currentSegment === targetSegment
        ? '#content-container,#toc-nav'
        : '#content-container,#toc-nav,#nav-tree,#nav-dropdown'
}

/**
 * Applies the appropriate htmx attributes to an anchor element.
 * Uses different oob swap strategies based on the paths:
 *
 * - For /docs or /docs/api/* (target or current): swap only #main-container
 * - For same top-level group: swap #content-container,#toc-nav
 * - For different top-level group: swap #content-container,#toc-nav,#nav-tree,#nav-dropdown
 */
export const applyHtmxAttributes = (
    anchor: HTMLAnchorElement,
    path: string,
    currentPathname: string
): void => {
    let hxSelectOob: string

    if (isSimpleSwapPath(path) || isSimpleSwapPath(currentPathname)) {
        // For /docs or /docs/api/* paths, only swap main container
        hxSelectOob = '#main-container'
    } else {
        // Use standard oob swap logic
        hxSelectOob = getHxSelectOob(path, currentPathname)
    }

    anchor.setAttribute('hx-select-oob', hxSelectOob)
    anchor.setAttribute('hx-swap', 'none')
}

/**
 * Hook that tracks the current pathname and updates when htmx navigation occurs.
 */
export const useCurrentPathname = (): string => {
    const [pathname, setPathname] = useState(window.location.pathname)

    useEffect(() => {
        const handleNavigation = () => {
            setPathname(window.location.pathname)
        }

        // Listen for htmx history updates (htmx navigation)
        document.addEventListener('htmx:pushedIntoHistory', handleNavigation)
        // Listen for browser back/forward navigation
        window.addEventListener('popstate', handleNavigation)

        return () => {
            document.removeEventListener(
                'htmx:pushedIntoHistory',
                handleNavigation
            )
            window.removeEventListener('popstate', handleNavigation)
        }
    }, [])

    return pathname
}
