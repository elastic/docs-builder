import { config } from './config'

export interface ApiTree {
    product: string
    /** `8` for `/v8/`, `main` for `/next/` (the unreleased spec), `null` on the unversioned path. */
    tree: string | null
}

const apiTreePath = /^\/api\/([^/]+)(?:\/(?:v(\d+)|(next)))?(?:\/|$)/

/** Reads an API page path under the site `root`, so a docs page with an `api` folder of its own never matches. */
export function parseApiTree(
    path: string,
    root = config.rootPath.replace(/\/$/, '')
): ApiTree | null {
    if (!path.startsWith(`${root}/`)) return null
    const match = apiTreePath.exec(path.slice(root.length))
    if (!match) return null
    // The search index names the unreleased tree `main`; only its URL segment is `next`.
    return { product: match[1], tree: match[2] ?? (match[3] ? 'main' : null) }
}

/**
 * The API page a search comes from: its key on any page of one API, which lifts that API's hits, plus the tree inside a
 * version tree, which also scopes them. Markdown pages and the catalog send nothing, so those requests stay unchanged.
 */
export function apiPageSearchParams(
    path: string,
    root?: string
): Record<string, string> {
    const parsed = parseApiTree(path, root)
    if (!parsed) return {}
    if (!parsed.tree) return { api_key: parsed.product }
    return { api_key: parsed.product, api_tree: parsed.tree }
}
