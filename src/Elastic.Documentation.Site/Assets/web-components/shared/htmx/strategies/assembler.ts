import { config } from '../../../../config'
import type { HtmxUrlStrategy } from './types'

// rootPath is '/docs' on prod but varies on PR previews (e.g.
// '/elastic/docs-builder/docs/3634'), so it can't be hardcoded.
const root = config.rootPath.replace(/\/$/, '')
const apiRoot = `${root}/api`
const isDocsPath = (path: string) =>
    path === root || path.startsWith(`${root}/`)

export const assemblerStrategy: HtmxUrlStrategy = {
    isExternalDocsUrl: (url) =>
        url === apiRoot || url.startsWith(`${apiRoot}/`),

    getPathFromUrl: (url) => {
        try {
            if (url.startsWith('/')) return isDocsPath(url) ? url : null
            const parsed = new URL(url)
            // elastic.co covers canonical links on prod; the origin check
            // covers absolute self-links on previews and white-label hosts.
            const isThisSite =
                parsed.hostname.endsWith('elastic.co') ||
                parsed.origin === window.location.origin
            if (isThisSite && isDocsPath(parsed.pathname)) {
                return parsed.pathname
            }
            return null
        } catch {
            return null
        }
    },
}
