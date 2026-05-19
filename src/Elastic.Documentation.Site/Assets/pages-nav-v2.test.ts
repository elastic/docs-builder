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
                                                    <input id="group-a-2" type="checkbox" checked />
                                                    <a href="/guide/a/topic-2" class="sidebar-link nav-v2-link">Topic 2</a>
                                                </div>
                                                <div class="docs-sidebar-nav-v2__folder-clip w-full">
                                                    <div class="docs-sidebar-nav-v2__folder-clip-inner">
                                                        <ul class="docs-sidebar-nav-v2__folder-children w-full relative">
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
                                    <input id="group-b" type="checkbox" checked />
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

    it('keeps only the active page branch expanded', () => {
        window.history.pushState({}, '', '/guide/a/topic-1/current-page')

        const nav = renderNav()

        initNavV2(nav)

        expect(checkbox('group-a').checked).toBe(true)
        expect(checkbox('group-a-1').checked).toBe(true)
        expect(checkbox('group-a-2').checked).toBe(false)
        expect(checkbox('group-b').checked).toBe(false)
    })

    it('collapses sibling folders when a new folder is opened manually', () => {
        window.history.pushState({}, '', '/guide/a/topic-1/current-page')

        const nav = renderNav()

        initNavV2(nav)

        const groupA = checkbox('group-a')
        const groupB = checkbox('group-b')
        groupB.checked = true
        groupB.dispatchEvent(new Event('change', { bubbles: true }))

        expect(groupB.checked).toBe(true)
        expect(groupA.checked).toBe(false)
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
})
