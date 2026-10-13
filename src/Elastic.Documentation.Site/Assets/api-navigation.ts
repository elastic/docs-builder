/**
 * Boosted navigation swaps only `#main-container`. Each API product brings its own context
 * bar, and `pages-nav.ts` narrows API-to-API swaps even further, to `#api-content-grid`. A move
 * that changes the page chrome therefore has to reload the page instead.
 */

/** The API product in an API path, `null` for the catalog, and `undefined` for any other path. */
function apiProduct(path: string, root: string): string | null | undefined {
    const api = `${root}/api`
    if (path !== api && !path.startsWith(`${api}/`)) return undefined
    return path.slice(api.length).match(/^\/doc\/([^/]+)/)?.[1] ?? null
}

export function changesPageChrome(
    targetPath: string,
    currentPath: string,
    root: string
): boolean {
    const target = apiProduct(targetPath, root)
    const current = apiProduct(currentPath, root)
    return !!target && !!current && target !== current
}
