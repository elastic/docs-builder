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
}
