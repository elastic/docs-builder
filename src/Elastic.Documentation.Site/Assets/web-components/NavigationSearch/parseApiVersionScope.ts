// Majors are one or two digits; longer numbers ("404", "500") are search terms, not versions.
const VERSION_TOKEN = /^v?(\d{1,2})(?:\.\d+|\.x)?$/i
// Mirrors ApiUrlBuilder.ProductSuffix: /api/doc/{key}/v{N}/ for released majors.
const PATH_VERSION = /\/api\/doc\/[^/]+\/(v\d+)(?:\/|$)/

export const parseApiVersionScope = (
    query: string,
    pathname: string
): { query: string; apiVersion: string } => {
    const tokens = query.split(/\s+/).filter(Boolean)
    for (const [index, token] of tokens.entries()) {
        const major = VERSION_TOKEN.exec(token)?.[1]
        if (!major) continue
        return {
            query: tokens.filter((_, i) => i !== index).join(' '),
            apiVersion: `v${major}`,
        }
    }

    return {
        query: tokens.join(' '),
        apiVersion: PATH_VERSION.exec(pathname)?.[1] ?? 'latest',
    }
}
