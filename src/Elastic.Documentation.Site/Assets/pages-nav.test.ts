import {
    collapseAllFolders,
    ensureSubtreeClips,
    initNav,
    CURRENT_COLOR_DELAY_MS,
    markCurrentPage,
    navSurfaceKey,
    settleCurrentPage,
    incomingNavSurfaceKey,
    pinPagesNavScroll,
    shouldRetargetArticleSwap,
    syncPagesNavFromResponse,
} from './pages-nav'

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

    afterEach(() => {
        jest.useRealTimers()
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

    it('does not recenter the sidebar after a same-tree swap', () => {
        jest.useFakeTimers()
        document.body.innerHTML = `
            <nav id="pages-nav">
                <div class="pages-nav-v2-shell" data-nav-heading="Reference">
                    <div class="pages-nav-v2__scroll" id="nav-scroll">
                        <ul id="nav-tree-ref">
                            <li><a class="sidebar-link current" href="/docs/a/">A</a></li>
                        </ul>
                    </div>
                </div>
            </nav>
        `
        const scroll = document.querySelector<HTMLElement>('#nav-scroll')!
        Object.defineProperty(scroll, 'scrollTop', {
            configurable: true,
            writable: true,
            value: 80,
        })

        pinPagesNavScroll()
        syncPagesNavFromResponse(
            `<html><body>${pagesNav('nav-tree-ref', 'Reference')}</body></html>`
        )
        scroll.scrollTop = 0
        initNav()
        initNav()
        jest.advanceTimersByTime(150)

        expect(scroll.scrollTop).toBe(80)
        jest.useRealTimers()
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

    it('starts collapsed after an island swap back to the same tree', () => {
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
        ).toBe(false)
    })

    it('reopens the current folder and its children after an island swap', () => {
        const path = window.location.pathname
        const guides = `
            <nav id="pages-nav">
                <div class="pages-nav-v2-shell" data-nav-heading="Get started">
                    <ul id="nav-tree-guides">
                        <li class="nav-folder">
                            <div class="peer nav-folder-peer">
                                <input id="fundamentals" type="checkbox">
                                <a class="sidebar-link current" href="${path}">Elastic fundamentals</a>
                            </div>
                            <ul class="nav-subtree"><li>Child page</li></ul>
                        </li>
                    </ul>
                </div>
            </nav>
        `
        document.body.innerHTML = guides
        initNav()

        syncPagesNavFromResponse(
            `<html><body>${pagesNav('nav-tree-ref', 'Reference')}</body></html>`
        )
        syncPagesNavFromResponse(`<html><body>${guides}</body></html>`)
        initNav()

        expect(
            document.querySelector<HTMLInputElement>('#fundamentals')?.checked
        ).toBe(true)
        expect(document.body.textContent).toContain('Child page')
        expect(
            document
                .querySelector('.nav-subtree-clip')
                ?.classList.contains('nav-subtree-clip--open')
        ).toBe(true)
    })

    it('reattaches a nested current page after an island swap', () => {
        const path = window.location.pathname
        const incoming = `
            <nav id="pages-nav">
                <div class="pages-nav-v2-shell" data-nav-heading="Deploy">
                    <ul id="nav-tree-deploy">
                        <li class="nav-folder">
                            <div class="peer nav-folder-peer">
                                <input id="deploy-manage" type="checkbox" checked>
                                <a class="sidebar-link" href="/docs/deploy-manage/">Deploy and manage</a>
                            </div>
                            <ul class="nav-subtree">
                                <li class="nav-folder">
                                    <div class="peer nav-folder-peer">
                                        <input id="deploy" type="checkbox" checked>
                                        <a class="sidebar-link" href="/docs/deploy-manage/deploy/">Deploy</a>
                                    </div>
                                    <ul class="nav-subtree">
                                        <li>
                                            <a class="sidebar-link current" href="${path}">Elastic Cloud</a>
                                        </li>
                                    </ul>
                                </li>
                                <li class="nav-folder">
                                    <div class="peer nav-folder-peer">
                                        <input id="other" type="checkbox">
                                        <a class="sidebar-link" href="/docs/other/">Other</a>
                                    </div>
                                    <ul class="nav-subtree"><li>Stay closed</li></ul>
                                </li>
                            </ul>
                        </li>
                    </ul>
                </div>
            </nav>
        `
        document.body.innerHTML = pagesNav('nav-tree-guides', 'Get started')
        initNav()

        syncPagesNavFromResponse(`<html><body>${incoming}</body></html>`)
        initNav()

        const current = document.querySelector(
            '#pages-nav a.sidebar-link.current'
        )
        expect(current?.textContent?.trim()).toBe('Elastic Cloud')
        expect(current?.isConnected).toBe(true)
        expect(
            document.querySelector<HTMLInputElement>('#deploy-manage')?.checked
        ).toBe(true)
        expect(
            document.querySelector<HTMLInputElement>('#deploy')?.checked
        ).toBe(true)
        expect(
            document.querySelector<HTMLInputElement>('#other')?.checked
        ).toBe(false)
    })
})

describe('markCurrentPage', () => {
    it('does not drop current when the active path is unchanged', () => {
        const path = window.location.pathname
        document.body.innerHTML = `
            <nav id="pages-nav">
                <div class="pages-nav-v2-shell" data-nav-heading="Reference">
                    <ul id="nav-tree-ref">
                        <li><a class="sidebar-link current" href="${path}">A</a></li>
                        <li><a class="sidebar-link" href="/other">B</a></li>
                    </ul>
                </div>
            </nav>
        `
        const current = document.querySelector('a.current')!
        const remove = jest.spyOn(current.classList, 'remove')

        markCurrentPage(document.querySelector('#pages-nav')!)

        expect(remove).not.toHaveBeenCalledWith('current')
        expect(current.classList.contains('current')).toBe(true)
    })

    it('moves current to the matching path', () => {
        const path = window.location.pathname
        document.body.innerHTML = `
            <nav id="pages-nav">
                <div class="pages-nav-v2-shell" data-nav-heading="Reference">
                    <ul id="nav-tree-ref">
                        <li><a class="sidebar-link current" href="/other">A</a></li>
                        <li><a class="sidebar-link" href="${path}">B</a></li>
                    </ul>
                </div>
            </nav>
        `

        markCurrentPage(document.querySelector('#pages-nav')!)

        expect(
            document
                .querySelector('a[href="/other"]')
                ?.classList.contains('current')
        ).toBe(false)
        expect(
            document
                .querySelector(`a[href="${path}"]`)
                ?.classList.contains('current')
        ).toBe(true)
        expect(
            document
                .querySelector(`a[href="${path}"]`)
                ?.classList.contains('nav-v2-current-ready')
        ).toBe(false)

        settleCurrentPage(document.querySelector('#pages-nav')!)
        expect(
            document
                .querySelector(`a[href="${path}"]`)
                ?.classList.contains('nav-v2-current-ready')
        ).toBe(true)
    })

    it('delays the current text color without applying it immediately', () => {
        jest.useFakeTimers()
        const path = window.location.pathname
        document.body.innerHTML = `
            <nav id="pages-nav">
                <ul id="nav-tree-ref">
                    <li><a class="sidebar-link current" href="${path}">A</a></li>
                </ul>
            </nav>
        `
        const nav = document.querySelector<HTMLElement>('#pages-nav')!
        const link = document.querySelector('a.current')!

        settleCurrentPage(nav, { delay: true })
        expect(link.classList.contains('nav-v2-current-ready')).toBe(false)

        jest.advanceTimersByTime(CURRENT_COLOR_DELAY_MS - 1)
        expect(link.classList.contains('nav-v2-current-ready')).toBe(false)

        jest.advanceTimersByTime(1)
        expect(link.classList.contains('nav-v2-current-ready')).toBe(true)
        jest.useRealTimers()
    })

    it('does not let a later immediate settle cancel the color delay', () => {
        jest.useFakeTimers()
        const path = window.location.pathname
        document.body.innerHTML = `
            <nav id="pages-nav">
                <ul id="nav-tree-ref">
                    <li><a class="sidebar-link current" href="${path}">A</a></li>
                </ul>
            </nav>
        `
        const nav = document.querySelector<HTMLElement>('#pages-nav')!
        const link = document.querySelector('a.current')!

        settleCurrentPage(nav, { delay: true })
        settleCurrentPage(nav)
        expect(link.classList.contains('nav-v2-current-ready')).toBe(false)

        jest.advanceTimersByTime(CURRENT_COLOR_DELAY_MS)
        expect(link.classList.contains('nav-v2-current-ready')).toBe(true)
        jest.useRealTimers()
    })
})

describe('collapseAllFolders', () => {
    beforeEach(() => {
        sessionStorage.clear()
    })

    it('collapses open folders when the section item is clicked', () => {
        document.body.innerHTML = `
            <nav id="secondary-nav">
                <li class="secondary-nav-item secondary-nav-item--active">
                    <a class="secondary-nav-item__hit" href="/docs/get-started">Guides</a>
                </li>
            </nav>
            ${pagesNav(
                'nav-tree-guides',
                'Get started',
                '<input id="folder-a" type="checkbox" checked>'
            )}
        `
        initNav()
        expect(
            document.querySelector<HTMLInputElement>('#folder-a')?.checked
        ).toBe(true)

        document
            .querySelector('#secondary-nav a.secondary-nav-item__hit')!
            .dispatchEvent(
                new MouseEvent('click', {
                    bubbles: true,
                    cancelable: true,
                    button: 0,
                })
            )

        expect(
            document.querySelector<HTMLInputElement>('#folder-a')?.checked
        ).toBe(true)

        document.querySelector<HTMLInputElement>('#folder-a')!.checked = true
        sessionStorage.setItem(
            `nav-expanded:${navSurfaceKey(document)}`,
            JSON.stringify(['folder-a'])
        )
        initNav()
        expect(
            document.querySelector<HTMLInputElement>('#folder-a')?.checked
        ).toBe(false)
    })

    it('clears persisted folder state so initNav does not reopen them', () => {
        document.body.innerHTML = pagesNav(
            'nav-tree-guides',
            'Get started',
            '<input id="folder-a" type="checkbox" checked>'
        )
        const nav = document.querySelector<HTMLElement>('#pages-nav')!
        sessionStorage.setItem(
            `nav-expanded:${navSurfaceKey(nav)}`,
            JSON.stringify(['folder-a'])
        )

        collapseAllFolders(nav)
        initNav()

        expect(
            document.querySelector<HTMLInputElement>('#folder-a')?.checked
        ).toBe(false)
    })
})

describe('ensureSubtreeClips', () => {
    beforeEach(() => {
        sessionStorage.clear()
    })

    it('leaves the legacy accordion alone when navigation-preview is off', () => {
        document.body.innerHTML = `
            <nav id="pages-nav">
                <li class="nav-folder">
                    <div class="peer nav-folder-peer">
                        <input id="folder-a" type="checkbox">
                    </div>
                    <ul class="nav-subtree"><li>Child</li></ul>
                </li>
            </nav>
        `
        initNav()
        const cb = document.querySelector<HTMLInputElement>('#folder-a')!
        cb.checked = true
        cb.dispatchEvent(new Event('change', { bubbles: true }))

        expect(document.querySelector('.nav-subtree-clip')).toBeNull()
        expect(
            document.querySelector('li.nav-folder > ul.nav-subtree')
        ).not.toBeNull()
        expect(document.body.textContent).toContain('Child')
    })

    it('wraps a folder subtree once', () => {
        document.body.innerHTML = `
            <nav id="pages-nav">
                <div class="pages-nav-v2-shell" data-nav-heading="Test">
                    <li class="nav-folder">
                        <div class="peer nav-folder-peer">
                            <input id="folder-a" type="checkbox">
                        </div>
                        <ul class="nav-subtree"><li>Child</li></ul>
                    </li>
                </div>
            </nav>
        `
        const nav = document.querySelector<HTMLElement>('#pages-nav')!
        ensureSubtreeClips(nav)
        ensureSubtreeClips(nav)

        expect(document.querySelectorAll('.nav-subtree-clip')).toHaveLength(1)
        expect(
            document.querySelector('.nav-subtree-clip > ul.nav-subtree')
                ?.textContent
        ).toContain('Child')
    })

    it('initNav wraps folders so open/close can animate without breaking layout', () => {
        document.body.innerHTML = `
            <nav id="pages-nav">
                <div class="pages-nav-v2-shell" data-nav-heading="Test">
                    <li class="nav-folder">
                        <div class="peer nav-folder-peer">
                            <input id="folder-a" type="checkbox">
                        </div>
                        <ul class="nav-subtree"><li>Child</li></ul>
                    </li>
                </div>
            </nav>
        `
        initNav()
        const cb = document.querySelector<HTMLInputElement>('#folder-a')!
        cb.checked = true
        cb.dispatchEvent(new Event('change', { bubbles: true }))
        expect(document.querySelectorAll('.nav-subtree-clip')).toHaveLength(1)
    })

    it('keeps a closed subtree in the document so the first open can animate', () => {
        document.body.innerHTML = `
            <nav id="pages-nav">
                <div class="pages-nav-v2-shell" data-nav-heading="Test">
                    <li class="nav-folder">
                        <div class="peer nav-folder-peer">
                            <input id="folder-a" type="checkbox">
                        </div>
                        <ul class="nav-subtree"><li>Child</li></ul>
                    </li>
                </div>
            </nav>
        `
        initNav()
        const folder = document.querySelector('li.nav-folder')!
        const clip = folder.querySelector('.nav-subtree-clip')
        expect(clip).not.toBeNull()
        expect(clip?.classList.contains('nav-subtree-clip--open')).toBe(false)

        const cb = document.querySelector<HTMLInputElement>('#folder-a')!
        cb.checked = true
        cb.dispatchEvent(new Event('change', { bubbles: true }))
        expect(folder.querySelector('.nav-subtree')?.textContent).toContain(
            'Child'
        )
        expect(folder.querySelector('.nav-subtree-clip')).toBe(clip)
    })

    it('does not yank a clip that is already in the document back to closed', () => {
        document.body.innerHTML = `
            <nav id="pages-nav">
                <div class="pages-nav-v2-shell" data-nav-heading="Test">
                    <li class="nav-folder">
                        <div class="peer nav-folder-peer">
                            <input id="folder-a" type="checkbox">
                        </div>
                        <ul class="nav-subtree"><li>Child</li></ul>
                    </li>
                </div>
            </nav>
        `
        initNav()
        const nav = document.querySelector<HTMLElement>('#pages-nav')!
        nav.classList.remove('nav-no-folder-anim')
        const cb = document.querySelector<HTMLInputElement>('#folder-a')!
        const clip = document.querySelector('.nav-subtree-clip')
        cb.checked = true
        cb.dispatchEvent(new Event('change', { bubbles: true }))
        expect(document.querySelector('.nav-subtree-clip')).toBe(clip)
        expect(clip?.textContent).toContain('Child')
    })

    it('does not snap a first-open animation when initNav re-runs', () => {
        document.body.innerHTML = `
            <nav id="pages-nav">
                <div class="pages-nav-v2-shell" data-nav-heading="Solutions">
                    <ul id="nav-tree-solutions">
                        <li class="nav-folder">
                            <div class="peer nav-folder-peer">
                                <input id="folder-a" type="checkbox">
                                <a class="sidebar-link" href="/docs/solutions/a">A</a>
                            </div>
                            <ul class="nav-subtree"><li>Child</li></ul>
                        </li>
                    </ul>
                </div>
            </nav>
        `
        initNav()
        const nav = document.querySelector<HTMLElement>('#pages-nav')!
        nav.classList.remove('nav-no-folder-anim')
        const clip = document.querySelector<HTMLElement>('.nav-subtree-clip')!
        const inner = clip.querySelector<HTMLElement>('.nav-subtree')!
        Object.defineProperty(inner, 'scrollHeight', {
            configurable: true,
            get: () => 80,
        })
        Object.defineProperty(inner, 'offsetHeight', {
            configurable: true,
            get: () => 80,
        })
        nav.classList.add('nav-no-folder-anim')
        clip.closest('li.nav-folder')!
            .querySelector('a.sidebar-link')!
            .dispatchEvent(
                new MouseEvent('click', {
                    bubbles: true,
                    cancelable: true,
                    button: 0,
                })
            )

        expect(clip.classList.contains('nav-subtree-clip--open')).toBe(false)
        expect(clip.style.height).toBe('80px')

        initNav()

        expect(clip.classList.contains('nav-subtree-clip--open')).toBe(false)
        expect(clip.style.height).toBe('80px')
    })

    it('animates the first open and keeps it after a same-tree initNav', () => {
        document.body.innerHTML = `
            <nav id="pages-nav">
                <div class="pages-nav-v2-shell" data-nav-heading="Solutions">
                    <ul id="nav-tree-solutions">
                        <li class="nav-folder">
                            <div class="peer nav-folder-peer">
                                <input id="folder-a" type="checkbox">
                                <a class="sidebar-link" href="/docs/solutions/a">A</a>
                            </div>
                            <ul class="nav-subtree">
                                <li class="nav-folder">
                                    <div class="peer nav-folder-peer">
                                        <input id="folder-b" type="checkbox">
                                        <a class="sidebar-link" href="/docs/solutions/b">B</a>
                                    </div>
                                    <ul class="nav-subtree"><li>Grandchild</li></ul>
                                </li>
                                <li>Leaf</li>
                            </ul>
                        </li>
                    </ul>
                </div>
            </nav>
        `
        initNav()
        const nav = document.querySelector<HTMLElement>('#pages-nav')!
        nav.classList.remove('nav-no-folder-anim')

        const cb = document.querySelector<HTMLInputElement>('#folder-a')!
        cb.checked = true
        cb.dispatchEvent(new Event('change', { bubbles: true }))

        const clip = document.querySelector('.nav-subtree-clip')
        expect(clip?.isConnected).toBe(true)
        expect(document.body.textContent).toContain('Leaf')
        expect(document.body.textContent).not.toContain('Grandchild')

        syncPagesNavFromResponse(
            `<html><body>${pagesNav('nav-tree-solutions', 'Solutions')}</body></html>`
        )
        initNav()

        expect(nav.classList.contains('nav-no-folder-anim')).toBe(false)
        expect(clip?.isConnected).toBe(true)
        expect(document.body.textContent).not.toContain('Grandchild')
    })

    it('keeps nested closed subtrees out of the document when the parent is open', () => {
        document.body.innerHTML = `
            <nav id="pages-nav">
                <div class="pages-nav-v2-shell" data-nav-heading="Test">
                    <li class="nav-folder">
                        <div class="peer nav-folder-peer">
                            <input id="parent" type="checkbox">
                        </div>
                        <ul class="nav-subtree">
                            <li class="nav-folder">
                                <div class="peer nav-folder-peer">
                                    <input id="child" type="checkbox">
                                </div>
                                <ul class="nav-subtree"><li>Grandchild</li></ul>
                            </li>
                            <li>Leaf</li>
                        </ul>
                    </li>
                </div>
            </nav>
        `
        initNav()
        const parent = document.querySelector<HTMLInputElement>('#parent')!
        parent.checked = true
        parent.dispatchEvent(new Event('change', { bubbles: true }))

        expect(document.body.textContent).toContain('Leaf')
        expect(document.body.textContent).not.toContain('Grandchild')
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

    it('matches the live key from the raw response without parsing', () => {
        const html = pagesNav('nav-tree-guides-outgoing', 'Get started')
        document.body.innerHTML = html
        expect(incomingNavSurfaceKey(`<html><body>${html}</body></html>`)).toBe(
            navSurfaceKey(document)
        )
        expect(incomingNavSurfaceKey('<html><body></body></html>')).toBe('')
    })
})

describe('shouldRetargetArticleSwap', () => {
    it('retargets when both pages are docs articles', () => {
        document.body.innerHTML =
            '<main id="content-container" class="min-w-0 md:col-start-2"></main>'
        expect(
            shouldRetargetArticleSwap(
                document.getElementById('content-container'),
                '<main id="content-container" class="min-w-0 md:col-start-2"></main>'
            )
        ).toBe(true)
    })

    it('keeps the full swap for a landing page without the article column', () => {
        document.body.innerHTML =
            '<main id="content-container" class="min-w-0 md:col-start-2"></main>'
        expect(
            shouldRetargetArticleSwap(
                document.getElementById('content-container'),
                '<div class="w-full" id="hero"></div>'
            )
        ).toBe(false)
    })
})
