/**
 * Boosted navigation swaps only `#main-container`, and the header sits outside it. Docs pages
 * and API pages bring different headers, so a move between them has to reload the page. The API
 * context bar lives inside `#main-container`, so a move between API products or versions does not.
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
    return (target === undefined) !== (current === undefined)
}

/** The product and version of an API path, such as `es` or `es/v9`. `null` outside an API product. */
function apiTree(path: string): string | null {
    const product = /\/api\/doc\/([^/]+)(?:\/(v\d+))?(?:\/|$)/.exec(path)
    return product ? `${product[1]}/${product[2] ?? ''}` : null
}

/**
 * A move to another product or version replaces the navigation tree and the context bar, which sit outside
 * `#api-content-grid`, so it must swap the whole `#main-container`.
 */
export function changesApiTree(
    targetPath: string,
    currentPath: string
): boolean {
    const target = apiTree(targetPath)
    const current = apiTree(currentPath)
    return target !== null && current !== null && target !== current
}
