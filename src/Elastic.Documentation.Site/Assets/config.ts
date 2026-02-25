/**
 * Build-type-specific configuration injected by the server.
 * Used by OTEL and HTMX utilities to vary behavior per build type.
 */
export type BuildType = 'assembler' | 'codex' | 'isolated'

export interface DocsConfig {
    buildType: BuildType
    serviceName: string
    telemetryEnabled: boolean
    rootPath: string // '/docs' for assembler, '' for codex and isolated
    apiBasePath: string // '/docs/_api/v1' for assembler, '/api/v1' for codex
}

declare global {
    interface Window {
        __DOCS_CONFIG__?: DocsConfig
    }
}

const DEFAULT_CONFIG: DocsConfig = {
    buildType: 'isolated',
    serviceName: 'docs-frontend',
    telemetryEnabled: false,
    rootPath: '',
    apiBasePath: '/docs/_api/v1',
}

export const config: DocsConfig = window.__DOCS_CONFIG__ ?? DEFAULT_CONFIG
