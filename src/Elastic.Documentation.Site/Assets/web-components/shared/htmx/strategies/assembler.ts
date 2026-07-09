import type { HtmxUrlStrategy } from './types'

export const assemblerStrategy: HtmxUrlStrategy = {
    isExternalDocsUrl: (url) =>
        url.startsWith('/docs/api/') || url === '/docs/api',

    getPathFromUrl: (url) => {
        try {
            const isDocsPath = (path: string) =>
                path === '/docs' || path.startsWith('/docs/')
            if (url.startsWith('/')) return isDocsPath(url) ? url : null
            const parsed = new URL(url)
            if (
                parsed.hostname.endsWith('elastic.co') &&
                isDocsPath(parsed.pathname)
            ) {
                return parsed.pathname
            }
            return null
        } catch {
            return null
        }
    },

    getFirstSegment: (path) => path.replace('/docs/', '/').split('/')[1] ?? '',

    isSimpleSwapPath: (path) => {
        const normalizedPath = path.endsWith('/') ? path.slice(0, -1) : path
        if (normalizedPath === '/docs') return true
        return path.startsWith('/docs/api/') || path === '/docs/api'
    },
}
