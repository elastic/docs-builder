export interface ApiTree {
    product: string
    /** `8` for `/v8/`, `main` for `/main/`, `null` on the unversioned path. */
    tree: string | null
}

const apiTreePath = /\/api\/doc\/([^/]+)(?:\/(?:v(\d+)|(main)))?(?:\/|$)/

export function parseApiTree(path: string): ApiTree | null {
    const match = apiTreePath.exec(path)
    if (!match) return null
    return { product: match[1], tree: match[2] ?? match[3] ?? null }
}

/**
 * The API page a search comes from: its key on any page of one API, which lifts that API's hits, plus the tree inside a
 * version tree, which also scopes them. Markdown pages and the catalog send nothing, so those requests stay unchanged.
 */
export function apiPageSearchParams(path: string): Record<string, string> {
    const parsed = parseApiTree(path)
    if (!parsed) return {}
    if (!parsed.tree) return { api_key: parsed.product }
    return { api_key: parsed.product, api_tree: parsed.tree }
}
