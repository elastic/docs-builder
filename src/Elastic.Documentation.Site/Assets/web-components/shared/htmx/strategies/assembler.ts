import { config } from '../../../../config'
import type { HtmxUrlStrategy } from './types'

// rootPath is '/docs' on prod but varies on PR previews (e.g.
// '/elastic/docs-builder/docs/3634'), so it can't be hardcoded.
const root = () => config.rootPath.replace(/\/$/, '')

export const assemblerStrategy: HtmxUrlStrategy = {
    isExternalDocsUrl: (url) => {
        const apiRoot = `${root()}/api`
        return url === apiRoot || url.startsWith(`${apiRoot}/`)
    },

    getPathFromUrl: (url) => {
        try {
            const isDocsPath = (path: string) =>
                path === root() || path.startsWith(`${root()}/`)
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
}
