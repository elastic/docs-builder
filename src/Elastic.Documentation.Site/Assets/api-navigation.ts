/**
 * Boosted navigation swaps only `#main-container`, and the header sits outside it. Docs pages
 * and API pages bring different headers, and each API product brings its own context bar, while
 * `pages-nav.ts` narrows API-to-API swaps to `#api-content-grid`. A move that changes the page
 * chrome therefore has to reload the page instead.
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
    // A docs page and an API page: the header would stay as the previous page had it.
    if ((target === undefined) !== (current === undefined)) return true
    return !!target && !!current && target !== current
}

/** `v9` for `/api/doc/es/v9/...`, an empty string for the unversioned `main` tree, `null` outside an API product. */
function apiVersion(path: string): string | null {
    const product = /\/api\/doc\/[^/]+(?:\/(v\d+))?(?:\/|$)/.exec(path)
    return product ? (product[1] ?? '') : null
}

/**
 * A move between versions of one API replaces the navigation tree and the version picker, which sit outside
 * `#api-content-grid`, so it must swap the whole `#main-container`.
 */
export function changesApiVersion(
    targetPath: string,
    currentPath: string
): boolean {
    const target = apiVersion(targetPath)
    const current = apiVersion(currentPath)
    return target !== null && current !== null && target !== current
}
