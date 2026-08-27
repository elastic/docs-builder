import { initNav, navSurfaceKey, syncPagesNavFromResponse } from './pages-nav'

function pagesNav(treeId: string, heading: string, extra = ''): string {
    const headingHtml = heading
        ? `<div class="pages-nav-v2__heading"><span class="pages-nav-v2__heading-text">${heading}</span></div>`
        : ''
    return `
        <nav id="pages-nav">
            <div class="pages-nav-v2-shell" data-nav-heading="${heading}">
                ${headingHtml}
                <ul id="${treeId}"></ul>
                ${extra}
            </div>
        </nav>
    `
}

describe('syncPagesNavFromResponse', () => {
    beforeEach(() => {
        sessionStorage.clear()
    })

    it('replaces the sidebar when the incoming page has a different heading', () => {
        document.body.innerHTML = pagesNav('nav-tree-ref', 'Reference')

        const replaced = syncPagesNavFromResponse(
            `<html><body>${pagesNav('nav-tree-es', 'Elasticsearch')}</body></html>`
        )

        expect(replaced).toBe(true)
        expect(
            document.querySelector('.pages-nav-v2__heading-text')?.textContent
        ).toBe('Elasticsearch')
        expect(document.querySelector('[id^="nav-tree-"]')?.id).toBe(
            'nav-tree-es'
        )
    })

    it('keeps the live sidebar when the heading and tree stay the same', () => {
        document.body.innerHTML = pagesNav(
            'nav-tree-ref',
            'Reference',
            '<input id="folder-a" type="checkbox" checked>'
        )
        const live = document.querySelector('#pages-nav')
        if (live) live.setAttribute('data-live', '1')

        const replaced = syncPagesNavFromResponse(
            `<html><body>${pagesNav('nav-tree-ref', 'Reference')}</body></html>`
        )

        expect(replaced).toBe(false)
        expect(
            document.querySelector('#pages-nav')?.getAttribute('data-live')
        ).toBe('1')
        expect(
            document.querySelector<HTMLInputElement>('#folder-a')?.checked
        ).toBe(true)
    })

    it('adopts the incoming node into the live document', () => {
        document.body.innerHTML = pagesNav('nav-tree-ref', 'Reference')
        const replaced = syncPagesNavFromResponse(
            `<html><body>${pagesNav('nav-tree-es', 'Elasticsearch')}</body></html>`
        )
        expect(replaced).toBe(true)
        expect(document.querySelector('#pages-nav')?.ownerDocument).toBe(
            document
        )
    })

    it('restores expanded folders after an island swap back to the same tree', () => {
        document.body.innerHTML = pagesNav(
            'nav-tree-ref',
            'Reference',
            '<input id="folder-a" type="checkbox" checked>'
        )
        initNav()

        syncPagesNavFromResponse(
            `<html><body>${pagesNav('nav-tree-es', 'Elasticsearch')}</body></html>`
        )
        syncPagesNavFromResponse(
            `<html><body>${pagesNav(
                'nav-tree-ref',
                'Reference',
                '<input id="folder-a" type="checkbox">'
            )}</body></html>`
        )
        initNav()

        expect(
            document.querySelector<HTMLInputElement>('#folder-a')?.checked
        ).toBe(true)
    })
})

describe('navSurfaceKey', () => {
    it('treats an outgoing tree id as the same island', () => {
        document.body.innerHTML = pagesNav(
            'nav-tree-es-outgoing',
            'Elasticsearch'
        )
        expect(navSurfaceKey(document)).toBe('nav-tree-es::Elasticsearch')
    })
})
