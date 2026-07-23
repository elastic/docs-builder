import { mapPagefindResults } from './pagefind'

describe('mapPagefindResults', () => {
    it('maps matching sections into modal results', () => {
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
                    sub_results: [
                        {
                            title: 'Install',
                            url: '/guide/#install',
                            excerpt: 'Run the <mark>installer</mark>',
                        },
                    ],
                },
            },
        ])

        expect(results).toEqual([
            {
                type: 'docs',
                url: '/guide/#install',
                title: 'Install',
                description: 'Run the <mark>installer</mark>',
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
})
