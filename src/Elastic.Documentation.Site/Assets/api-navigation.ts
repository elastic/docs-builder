import { parseApiTree } from './api-tree'

/**
 * Boosted navigation swaps only `#main-container`, and the header sits outside it. Docs pages
 * and API pages bring different headers, so a move between them has to reload the page. The API
 * identity selector lives in the sidebar inside `#main-container`, so a move between API products or versions does not.
 */

function isApiPath(path: string, root: string): boolean {
    const api = `${root}/api`
    return path === api || path.startsWith(`${api}/`)
}

export function changesPageChrome(
    targetPath: string,
    currentPath: string,
    root: string
): boolean {
    // A docs page and an API page: the header would stay as the previous page had it.
    return isApiPath(targetPath, root) !== isApiPath(currentPath, root)
}

/** The product and version of an API path, such as `es`, `es/v9` or `es/main`. `null` outside an API product. */
function apiTree(path: string): string | null {
    const parsed = parseApiTree(path)
    if (!parsed) return null
    const { product, tree } = parsed
    const segment = tree === null || tree === 'main' ? (tree ?? '') : `v${tree}`
    return `${product}/${segment}`
}

/**
 * A move to another product or version replaces the navigation tree and the identity selector, which sit outside
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
