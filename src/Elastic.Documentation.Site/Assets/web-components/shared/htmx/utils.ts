import { urlStrategy } from './urlStrategy'

export const isExternalDocsUrl = (url: string): boolean =>
    urlStrategy.isExternalDocsUrl(url)

export const getPathFromUrl = (url: string): string | null =>
    urlStrategy.getPathFromUrl(url)
