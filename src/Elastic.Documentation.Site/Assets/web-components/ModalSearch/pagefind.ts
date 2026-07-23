import { config } from '../../config'

interface PagefindResultData {
    url: string
    excerpt: string
    meta: {
        title?: string
    }
    sub_results?: Array<{
        title: string
        url: string
        excerpt: string
    }>
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

export interface StaticSearchResult {
    type: 'docs'
    url: string
    title: string
    description: string
    score: number
    parents: Array<{ url: string; title: string }>
}

export interface PagefindLoadedResult {
    score: number
    data: PagefindResultData
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
    pages.map(({ score, data }) => {
        const section = data.sub_results?.find(({ url }) =>
            url.includes('#')
        ) ??
            data.sub_results?.[0] ?? {
                title: data.meta.title ?? data.url,
                url: data.url,
                excerpt: data.excerpt,
            }

        return {
            type: 'docs' as const,
            url: section.url,
            title: section.title || data.meta.title || data.url,
            description: section.excerpt,
            score,
            parents: [],
        }
    })
