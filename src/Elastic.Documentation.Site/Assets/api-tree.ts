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

/** The search scope for a path inside one version tree, and nothing anywhere else so those requests stay unchanged. */
export function apiTreeSearchParams(path: string): Record<string, string> {
    const parsed = parseApiTree(path)
    if (!parsed?.tree) return {}
    return { api_key: parsed.product, api_tree: parsed.tree }
}
