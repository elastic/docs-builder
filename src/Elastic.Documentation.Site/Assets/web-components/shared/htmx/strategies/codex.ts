import type { HtmxUrlStrategy } from './types'

export const codexStrategy: HtmxUrlStrategy = {
    isExternalDocsUrl: () => false,

    getPathFromUrl: (url) => {
        try {
            if (url.startsWith('/')) return url
            const parsed = new URL(url)
            return parsed.origin === window.location.origin
                ? parsed.pathname
                : null
        } catch {
            return null
        }
    },

    getFirstSegment: (path) => {
        const rMatch = path.match(/^\/r\/([^/]+)/)
        if (rMatch) return rMatch[1]
        const gMatch = path.match(/^\/g\/([^/]+)/)
        if (gMatch) return gMatch[1]
        return path.split('/').filter(Boolean)[0] ?? ''
    },

    isSimpleSwapPath: (path) => {
        const normalizedPath = path.endsWith('/') ? path.slice(0, -1) : path
        if (normalizedPath === '' || normalizedPath === '/') return true
        return path.startsWith('/g/')
    },
}
