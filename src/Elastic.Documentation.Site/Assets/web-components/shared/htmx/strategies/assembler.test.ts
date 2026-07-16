import { assemblerStrategy } from './assembler'

// PR previews serve assembler under a prefix like '/elastic/docs-builder/docs/3634'
// instead of '/docs' — the strategy must key off config.rootPath, not a hardcoded '/docs'.
jest.mock('../../../../config', () => ({
    config: { rootPath: '/elastic/docs-builder/docs/3634' },
}))

describe('assemblerStrategy with a non-default rootPath', () => {
    it('treats rootPath-prefixed paths as docs paths', () => {
        expect(
            assemblerStrategy.getPathFromUrl(
                '/elastic/docs-builder/docs/3634/get-started'
            )
        ).toBe('/elastic/docs-builder/docs/3634/get-started')
    })

    it('rejects paths outside rootPath', () => {
        expect(assemblerStrategy.getPathFromUrl('/pricing')).toBe(null)
    })

    it('still excludes the /api sub-app under the prefixed root', () => {
        expect(
            assemblerStrategy.isExternalDocsUrl(
                '/elastic/docs-builder/docs/3634/api/elasticsearch'
            )
        ).toBe(true)
    })

    it('accepts absolute self-links on non-elastic.co hosts (previews)', () => {
        // jsdom origin is http://localhost
        expect(
            assemblerStrategy.getPathFromUrl(
                'http://localhost/elastic/docs-builder/docs/3634/get-started'
            )
        ).toBe('/elastic/docs-builder/docs/3634/get-started')
        expect(
            assemblerStrategy.getPathFromUrl(
                'https://some-other-host.dev/elastic/docs-builder/docs/3634/get-started'
            )
        ).toBe(null)
    })
})
