/**
 * Strategy for HTMX URL handling. Implementations vary by build type
 * (assembler, codex, isolated) due to different URL structures.
 */
export interface HtmxUrlStrategy {
    isExternalDocsUrl(url: string): boolean
    getPathFromUrl(url: string): string | null
    getFirstSegment(path: string): string
    isSimpleSwapPath(path: string): boolean
}
