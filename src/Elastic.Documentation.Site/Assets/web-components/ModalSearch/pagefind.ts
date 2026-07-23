import { config } from '../../config'

interface PagefindResultData {
    url: string
    excerpt: string
    meta: {
        title?: string
        breadcrumbs?: string
    }
}

interface PagefindRawResult {
    score: number
    data: () => Promise<PagefindResultData>
}

interface PagefindSearch {
    results: PagefindRawResult[]
}

interface PagefindApi {
    options: (options: { baseUrl: string; basePath: string }) => Promise<void>
    init: () => Promise<void>
    search: (query: string) => Promise<PagefindSearch | null>
}

interface StaticSearchResult {
    type: 'docs'
    url: string
    title: string
    description: string
    score: number
    parents: Array<{ url: string; title: string }>
}

interface PagefindLoadedResult {
    score: number
    data: PagefindResultData
}

interface StructuredBreadcrumbs {
    itemListElement?: Array<{
        name?: string
        item?: string
    }>
}

let pagefindPromise: Promise<PagefindApi> | undefined

const rootPath = config.rootPath.replace(/\/$/, '')

const loadPagefind = () => {
    pagefindPromise ??= (async () => {
        try {
            const moduleUrl = `${rootPath}/pagefind/pagefind.js`
            const pagefind = (await import(moduleUrl)) as PagefindApi
            await pagefind.options({
                baseUrl: rootPath || '/',
                basePath: `${rootPath}/pagefind/`,
            })
            await pagefind.init()
            return pagefind
        } catch (error) {
            pagefindPromise = undefined
            throw error
        }
    })()
    return pagefindPromise
}

export const searchPagefind = async (
    query: string
): Promise<StaticSearchResult[]> => {
    const pagefind = await loadPagefind()
    const search = await pagefind.search(query)
    if (!search) return []

    const pages = await Promise.all(
        search.results.slice(0, 20).map(async (result) => ({
            score: result.score,
            data: await result.data(),
        }))
    )

    return mapPagefindResults(pages)
}

export const mapPagefindResults = (
    pages: PagefindLoadedResult[]
): StaticSearchResult[] =>
    pages.map(({ score, data }) => ({
        type: 'docs' as const,
        url: data.url,
        title: data.meta.title || data.url,
        description: data.excerpt,
        score,
        parents: parseBreadcrumbs(data.meta.breadcrumbs),
    }))

const parseBreadcrumbs = (value?: string) => {
    if (!value) return []

    try {
        const breadcrumbs = JSON.parse(value) as StructuredBreadcrumbs
        return (breadcrumbs.itemListElement ?? [])
            .filter(
                (item): item is { name: string; item: string } =>
                    !!item.name && !!item.item
            )
            .map(({ name, item }) => ({ title: name, url: item }))
    } catch {
        return []
    }
}
