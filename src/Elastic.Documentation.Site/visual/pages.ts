export type HostName = 'isolated' | 'assembler' | 'codex'

export const hostPorts: Record<HostName, number> = {
    isolated: 4101,
    assembler: 4102,
    codex: 4103,
}

export const viewports = {
    desktop: { width: 1440, height: 900 },
    tablet: { width: 760, height: 1024 },
}

// Paths are relative to each host's root. Assembler pages live under /docs.
export const pages: Record<HostName, string[]> = {
    isolated: [
        '/',
        '/syntax/admonitions',
        '/syntax/tabs',
        '/syntax/code',
        '/syntax/stepper',
        '/syntax/dropdowns',
        '/syntax/tables',
        '/syntax/images',
        '/syntax/applies',
        '/syntax/card-group',
        '/syntax/hero',
        '/syntax/hub-pages',
        '/syntax/buttons',
        '/syntax/kbd',
        '/syntax/footnotes',
        '/release-notes',
        '/api',
        '/this-does-not-exist',
    ],
    assembler: [
        '/docs/',
        '/docs/get-started',
        '/docs/solutions/search',
        '/docs/reference/elasticsearch',
        '/docs/explore-analyze',
        '/docs/release-notes',
        '/docs/archive',
        '/docs/this-does-not-exist',
    ],
    // Codex content can be private, so no Codex page is named here. Set VISUAL_CODEX_PAGES (comma-separated paths) together with VISUAL_CODEX_CONFIG.
    codex: (process.env.VISUAL_CODEX_PAGES ?? '')
        .split(',')
        .map((p) => p.trim())
        .filter(Boolean),
}

export function slug(path: string): string {
    const trimmed = path.replace(/^\/|\/$/g, '')
    return trimmed === '' ? 'home' : trimmed.replace(/\//g, '-')
}
