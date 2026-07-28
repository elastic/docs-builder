import { createPagefindOptions, mapPagefindResults } from './pagefind'

describe('createPagefindOptions', () => {
    it('does not prepend the deployment root to indexed URLs', () => {
        expect(
            createPagefindOptions('/elastic/docs-builder/pull/3709')
        ).toEqual({
            baseUrl: '/',
            basePath: '/elastic/docs-builder/pull/3709/_static/pagefind/',
        })
    })
})

describe('mapPagefindResults', () => {
    it('maps matching pages into modal results', () => {
        const results = mapPagefindResults([
            {
                score: 0.9,
                data: {
                    url: '/guide/',
                    excerpt: 'Guide excerpt',
                    meta: {
                        title: 'Guide',
                        breadcrumbs: JSON.stringify({
                            itemListElement: [
                                {
                                    name: 'Docs',
                                    item: 'https://example.com/',
                                },
                                {
                                    name: 'Syntax guide',
                                    item: 'https://example.com/syntax',
                                },
                                {
                                    name: 'Mermaid diagrams',
                                },
                            ],
                        }),
                    },
                },
            },
        ])

        expect(results).toEqual([
            {
                type: 'docs',
                url: '/guide/',
                title: 'Guide',
                description: 'Guide excerpt',
                score: 0.9,
                parents: [
                    {
                        title: 'Docs',
                        url: 'https://example.com/',
                    },
                    {
                        title: 'Syntax guide',
                        url: 'https://example.com/syntax',
                    },
                ],
            },
        ])
    })

    it('falls back to page metadata when no sections are returned', () => {
        const [result] = mapPagefindResults([
            {
                score: 0.5,
                data: {
                    url: '/guide/',
                    excerpt: 'Guide excerpt',
                    meta: { title: 'Guide' },
                },
            },
        ])

        expect(result.title).toBe('Guide')
        expect(result.description).toBe('Guide excerpt')
    })

    it('falls back to the page URL when title metadata is missing', () => {
        const [result] = mapPagefindResults([
            {
                score: 0.5,
                data: {
                    url: '/guide/',
                    excerpt: 'Guide excerpt',
                    meta: {},
                },
            },
        ])

        expect(result.title).toBe('/guide/')
        expect(result.url).toBe('/guide/')
    })
})
