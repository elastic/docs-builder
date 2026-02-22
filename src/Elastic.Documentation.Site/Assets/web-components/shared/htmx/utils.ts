import { config } from '../../../config'
import { urlStrategy } from './urlStrategy'
import { useEffect, useState } from 'react'

export const isExternalDocsUrl = (url: string): boolean =>
    urlStrategy.isExternalDocsUrl(url)

export const getPathFromUrl = (url: string): string | null =>
    urlStrategy.getPathFromUrl(url)

/**
 * Returns the appropriate hx-select-oob value based on whether
 * the target URL is in the same top-level group as the current URL.
 * Includes #codex-breadcrumbs for codex builds so the sub-header updates on navigation.
 */
const getHxSelectOob = (targetUrl: string, currentPathname: string): string => {
    const currentSegment = urlStrategy.getFirstSegment(currentPathname)
    const targetSegment = urlStrategy.getFirstSegment(targetUrl)
    const base =
        currentSegment === targetSegment
            ? '#content-container,#toc-nav'
            : '#content-container,#toc-nav,#nav-tree,#nav-dropdown'
    return config.buildType === 'codex' ? `${base},#codex-breadcrumbs` : base
}

/**
 * Applies the appropriate htmx attributes to an anchor element.
 * Uses different oob swap strategies based on the paths:
 *
 * - For simple swap paths (landing, /docs/api/*, /g/*): swap only #main-container
 * - For same top-level group: swap #content-container,#toc-nav
 * - For different top-level group: swap #content-container,#toc-nav,#nav-tree,#nav-dropdown
 */
export const applyHtmxAttributes = (
    anchor: HTMLAnchorElement,
    path: string,
    currentPathname: string
): void => {
    const hxSelectOob =
        urlStrategy.isSimpleSwapPath(path) ||
        urlStrategy.isSimpleSwapPath(currentPathname)
            ? '#main-container'
            : getHxSelectOob(path, currentPathname)

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

        document.addEventListener('htmx:pushedIntoHistory', handleNavigation)
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
