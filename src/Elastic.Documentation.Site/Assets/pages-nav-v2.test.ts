import { initNavV2 } from './pages-nav-v2'

jest.mock('tippy.js', () => ({
    __esModule: true,
    default: jest.fn(() => ({
        destroy: jest.fn(),
        setContent: jest.fn(),
    })),
}))

function renderNav() {
    document.body.innerHTML = `
        <div class="pages-nav-menu">
            <nav class="docs-sidebar-nav-v2" data-nav-v2>
                <ul class="docs-sidebar-nav-v2__tree" id="nav-tree">
                    <li class="relative">
                        <span class="docs-sidebar-nav-v2__label docs-sidebar-nav-v2__label--top">Guides</span>
                        <ul class="docs-sidebar-nav-v2__label-children w-full">
                            <li class="flex flex-wrap group-navigation relative">
                                <div class="peer nav-folder-peer grid w-full grid-cols-1">
                                    <input id="group-a" type="checkbox" checked />
                                    <a href="/guide/a" class="sidebar-link nav-v2-link">Group A</a>
                                </div>
                                <div class="docs-sidebar-nav-v2__folder-clip w-full">
                                    <div class="docs-sidebar-nav-v2__folder-clip-inner">
                                        <ul class="docs-sidebar-nav-v2__folder-children w-full relative">
                                            <li class="flex flex-wrap group-navigation relative">
                                                <div class="peer nav-folder-peer grid w-full grid-cols-1">
                                                    <input id="group-a-1" type="checkbox" checked />
                                                    <a href="/guide/a/topic-1" class="sidebar-link nav-v2-link">Topic 1</a>
                                                </div>
                                                <div class="docs-sidebar-nav-v2__folder-clip w-full">
                                                    <div class="docs-sidebar-nav-v2__folder-clip-inner">
                                                        <ul class="docs-sidebar-nav-v2__folder-children w-full relative">
                                                            <li class="flex group/li">
                                                                <a href="/guide/a/topic-1/current-page" class="sidebar-link nav-v2-link">Current page</a>
                                                            </li>
                                                        </ul>
                                                    </div>
                                                </div>
                                            </li>
                                            <li class="flex flex-wrap group-navigation relative">
                                                <div class="peer nav-folder-peer grid w-full grid-cols-1">
                                                    <input id="group-a-2" type="checkbox" checked data-nav-v2-expanded-default="true" />
                                                    <a href="/guide/a/topic-2" class="sidebar-link nav-v2-link">Topic 2</a>
                                                </div>
                                                <div class="docs-sidebar-nav-v2__folder-clip w-full">
                                                    <div class="docs-sidebar-nav-v2__folder-clip-inner">
                                                        <ul class="docs-sidebar-nav-v2__folder-children w-full relative">
                                                            <li class="flex flex-wrap group-navigation relative">
                                                                <div class="peer nav-folder-peer grid w-full grid-cols-1">
                                                                    <input id="group-a-2-child" type="checkbox" checked />
                                                                    <a href="/guide/a/topic-2/nested" class="sidebar-link nav-v2-link">Nested topic</a>
                                                                </div>
                                                                <div class="docs-sidebar-nav-v2__folder-clip w-full">
                                                                    <div class="docs-sidebar-nav-v2__folder-clip-inner">
                                                                        <ul class="docs-sidebar-nav-v2__folder-children w-full relative">
                                                                            <li class="flex group/li">
                                                                                <a href="/guide/a/topic-2/nested/page" class="sidebar-link nav-v2-link">Nested page</a>
                                                                            </li>
                                                                        </ul>
                                                                    </div>
                                                                </div>
                                                            </li>
                                                            <li class="flex group/li">
                                                                <a href="/guide/a/topic-2/other-page" class="sidebar-link nav-v2-link">Other page</a>
                                                            </li>
                                                        </ul>
                                                    </div>
                                                </div>
                                            </li>
                                        </ul>
                                    </div>
                                </div>
                            </li>
                            <li class="flex flex-wrap group-navigation relative">
                                <div class="peer nav-folder-peer grid w-full grid-cols-1">
                                    <input id="group-b" type="checkbox" checked data-nav-v2-expanded-default="true" />
                                    <a href="/guide/b" class="sidebar-link nav-v2-link">Group B</a>
                                </div>
                                <div class="docs-sidebar-nav-v2__folder-clip w-full">
                                    <div class="docs-sidebar-nav-v2__folder-clip-inner">
                                        <ul class="docs-sidebar-nav-v2__folder-children w-full relative">
                                            <li class="flex group/li">
                                                <a href="/guide/b/page" class="sidebar-link nav-v2-link">Group B page</a>
                                            </li>
                                        </ul>
                                    </div>
                                </div>
                            </li>
                            <li class="flex flex-wrap group-navigation relative">
                                <div class="peer nav-folder-peer grid w-full grid-cols-1">
                                    <input id="group-c" type="checkbox" checked />
                                    <a href="/guide/c" class="sidebar-link nav-v2-link">Group C</a>
                                </div>
                                <div class="docs-sidebar-nav-v2__folder-clip w-full">
                                    <div class="docs-sidebar-nav-v2__folder-clip-inner">
                                        <ul class="docs-sidebar-nav-v2__folder-children w-full relative">
                                            <li class="flex group/li">
                                                <a href="/guide/c/page" class="sidebar-link nav-v2-link">Group C page</a>
                                            </li>
                                        </ul>
                                    </div>
                                </div>
                            </li>
                            <li class="flex flex-wrap group-navigation relative">
                                <div class="peer nav-folder-peer grid w-full grid-cols-1">
                                    <input id="duplicate-primary" type="checkbox" checked />
                                    <a href="/guide/fundamentals" data-nav-v2-location="3" class="sidebar-link nav-v2-link">Fundamentals</a>
                                </div>
                                <div class="docs-sidebar-nav-v2__folder-clip w-full">
                                    <div class="docs-sidebar-nav-v2__folder-clip-inner">
                                        <ul class="docs-sidebar-nav-v2__folder-children w-full relative">
                                            <li class="flex group/li">
                                                <a href="/guide/shared" data-nav-v2-location="3.0" class="sidebar-link nav-v2-link">Shared page in fundamentals</a>
                                            </li>
                                        </ul>
                                    </div>
                                </div>
                            </li>
                            <li class="flex flex-wrap group-navigation relative">
                                <div class="peer nav-folder-peer grid w-full grid-cols-1">
                                    <input id="duplicate-secondary" type="checkbox" checked data-nav-v2-expanded-default="true" />
                                    <a href="/guide/platform" data-nav-v2-location="4" class="sidebar-link nav-v2-link">Platform</a>
                                </div>
                                <div class="docs-sidebar-nav-v2__folder-clip w-full">
                                    <div class="docs-sidebar-nav-v2__folder-clip-inner">
                                        <ul class="docs-sidebar-nav-v2__folder-children w-full relative">
                                            <li class="flex flex-wrap group-navigation relative">
                                                <div class="peer nav-folder-peer grid w-full grid-cols-1">
                                                    <input id="duplicate-secondary-child" type="checkbox" checked />
                                                    <a href="/guide/platform/ingest" data-nav-v2-location="4.0" class="sidebar-link nav-v2-link">Ingest or migrate data</a>
                                                </div>
                                                <div class="docs-sidebar-nav-v2__folder-clip w-full">
                                                    <div class="docs-sidebar-nav-v2__folder-clip-inner">
                                                        <ul class="docs-sidebar-nav-v2__folder-children w-full relative">
                                                            <li class="flex group/li">
                                                                <a href="/guide/shared" data-nav-v2-location="4.0.0" class="sidebar-link nav-v2-link">Shared page in platform</a>
                                                            </li>
                                                        </ul>
                                                    </div>
                                                </div>
                                            </li>
                                        </ul>
                                    </div>
                                </div>
                            </li>
                            <li class="flex flex-wrap group-navigation relative">
                                <div class="peer nav-folder-peer grid w-full grid-cols-1">
                                    <input id="duplicate-folder-primary" type="checkbox" checked />
                                    <a href="/guide/shared-folder" data-nav-v2-location="5" class="sidebar-link nav-v2-link">Shared folder primary</a>
                                </div>
                                <div class="docs-sidebar-nav-v2__folder-clip w-full">
                                    <div class="docs-sidebar-nav-v2__folder-clip-inner">
                                        <ul class="docs-sidebar-nav-v2__folder-children w-full relative">
                                            <li class="flex group/li">
                                                <a href="/guide/shared-folder/child" data-nav-v2-location="5.0" class="sidebar-link nav-v2-link">Shared folder child</a>
                                            </li>
                                        </ul>
                                    </div>
                                </div>
                            </li>
                            <li class="flex flex-wrap group-navigation relative">
                                <div class="peer nav-folder-peer grid w-full grid-cols-1">
                                    <input id="duplicate-folder-secondary" type="checkbox" checked />
                                    <a href="/guide/shared-folder" data-nav-v2-location="6" class="sidebar-link nav-v2-link">Shared folder secondary</a>
                                </div>
                                <div class="docs-sidebar-nav-v2__folder-clip w-full">
                                    <div class="docs-sidebar-nav-v2__folder-clip-inner">
                                        <ul class="docs-sidebar-nav-v2__folder-children w-full relative">
                                            <li class="flex group/li">
                                                <a href="/guide/shared-folder/other-child" data-nav-v2-location="6.0" class="sidebar-link nav-v2-link">Shared folder other child</a>
                                            </li>
                                        </ul>
                                    </div>
                                </div>
                            </li>
                        </ul>
                    </li>
                </ul>
            </nav>
        </div>
    `

    return document.querySelector<HTMLElement>('[data-nav-v2]')!
}

function checkbox(id: string): HTMLInputElement {
    return document.getElementById(id) as HTMLInputElement
}

function groupRow(id: string): HTMLElement {
    return checkbox(id).closest('li.group-navigation') as HTMLElement
}

function currentLinks(nav: HTMLElement): string[] {
    return Array.from(nav.querySelectorAll('a.sidebar-link.current')).map(
        (a) => a.textContent?.trim() ?? ''
    )
}

describe('initNavV2', () => {
    const originalRequestAnimationFrame = window.requestAnimationFrame

    beforeEach(() => {
        sessionStorage.clear()
        window.requestAnimationFrame = ((cb: FrameRequestCallback) => {
            cb(0)
            return 0
        }) as typeof window.requestAnimationFrame
    })

    afterEach(() => {
        document.body.innerHTML = ''
    })

    afterAll(() => {
        window.requestAnimationFrame = originalRequestAnimationFrame
    })

    it('keeps the active page branch and default-expanded folders expanded', () => {
        window.history.pushState({}, '', '/guide/a/topic-1/current-page')

        const nav = renderNav()

        initNavV2(nav)

        expect(checkbox('group-a').checked).toBe(true)
        expect(checkbox('group-a-1').checked).toBe(true)
        expect(checkbox('group-a-2').checked).toBe(true)
        expect(checkbox('group-a-2-child').checked).toBe(false)
        expect(checkbox('group-b').checked).toBe(true)
        expect(checkbox('group-c').checked).toBe(false)
    })

    it('collapses non-default sibling folders when a new folder is opened manually', () => {
        window.history.pushState({}, '', '/guide/a/topic-1/current-page')

        const nav = renderNav()

        initNavV2(nav)

        const groupA = checkbox('group-a')
        const groupB = checkbox('group-b')
        const groupC = checkbox('group-c')
        groupC.checked = true
        groupC.dispatchEvent(new Event('change', { bubbles: true }))

        expect(groupC.checked).toBe(true)
        expect(groupA.checked).toBe(false)
        expect(groupB.checked).toBe(true)
    })

    it('respects manually collapsed default-expanded folders', () => {
        window.history.pushState({}, '', '/guide/a/topic-1/current-page')
        sessionStorage.setItem(
            'docs-builder-nav-v2-collapsed-ids',
            JSON.stringify(['group-b'])
        )

        const nav = renderNav()

        initNavV2(nav)

        const groupB = checkbox('group-b')

        expect(groupB.checked).toBe(false)
    })

    it('scrolls the active branch into view when it opens below the viewport', () => {
        window.history.pushState({}, '', '/guide/b/page')

        const nav = renderNav()
        const container = document.querySelector(
            '.pages-nav-menu'
        ) as HTMLElement
        const groupB = groupRow('group-b')

        Object.defineProperty(container, 'scrollTop', {
            value: 0,
            writable: true,
            configurable: true,
        })
        Object.defineProperty(container, 'clientHeight', {
            value: 120,
            configurable: true,
        })
        container.getBoundingClientRect = jest.fn(() => ({
            x: 0,
            y: 0,
            top: 0,
            right: 280,
            bottom: 120,
            left: 0,
            width: 280,
            height: 120,
            toJSON: () => ({}),
        }))
        groupB.getBoundingClientRect = jest.fn(() => ({
            x: 0,
            y: 150,
            top: 150,
            right: 280,
            bottom: 250,
            left: 0,
            width: 280,
            height: 100,
            toJSON: () => ({}),
        }))

        initNavV2(nav)

        expect(container.scrollTop).toBe(142)
    })

    it('focuses the canonical duplicate location on direct page load', () => {
        window.history.pushState({}, '', '/guide/shared')

        const nav = renderNav()

        initNavV2(nav)

        expect(currentLinks(nav)).toEqual(['Shared page in fundamentals'])
        expect(checkbox('duplicate-primary').checked).toBe(true)
        expect(checkbox('duplicate-secondary').checked).toBe(false)
        expect(checkbox('duplicate-secondary-child').checked).toBe(false)
    })

    it('focuses the clicked duplicate location after navigation', () => {
        window.history.pushState({}, '', '/guide/start')

        const nav = renderNav()
        const platformDuplicate = nav.querySelector<HTMLAnchorElement>(
            'a[data-nav-v2-location="4.0.0"]'
        )!

        platformDuplicate.click()
        window.history.pushState({}, '', '/guide/shared')
        initNavV2(nav)

        expect(currentLinks(nav)).toEqual(['Shared page in platform'])
        expect(checkbox('duplicate-primary').checked).toBe(false)
        expect(checkbox('duplicate-secondary').checked).toBe(true)
        expect(checkbox('duplicate-secondary-child').checked).toBe(true)
    })

    it('focuses a duplicate folder row when already on its page', () => {
        window.history.pushState({}, '', '/guide/shared-folder')

        const nav = renderNav()
        initNavV2(nav)

        expect(currentLinks(nav)).toEqual(['Shared folder primary'])

        const secondaryFolder = nav.querySelector<HTMLAnchorElement>(
            'a[data-nav-v2-location="6"]'
        )!

        secondaryFolder.click()

        expect(currentLinks(nav)).toEqual(['Shared folder secondary'])
        expect(checkbox('duplicate-folder-primary').checked).toBe(false)
        expect(checkbox('duplicate-folder-secondary').checked).toBe(true)
    })
})
