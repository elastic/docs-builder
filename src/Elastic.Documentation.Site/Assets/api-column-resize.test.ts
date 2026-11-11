import { clampWidth, initApiColumnResize } from './api-column-resize'

const rect = (left: number, width: number) =>
    ({ left, right: left + width, width }) as DOMRect

function mountShell() {
    document.body.innerHTML = `<div class="api-page-shell layout--three">
        <aside class="layout__nav"></aside>
        <aside class="api-page-rail"></aside>
    </div>`
    const shell = document.querySelector<HTMLElement>('.api-page-shell')!
    shell.getBoundingClientRect = () => rect(0, 1600)
    document.querySelector('.layout__nav')!.getBoundingClientRect = () =>
        rect(0, 300)
    document.querySelector('.api-page-rail')!.getBoundingClientRect = () =>
        rect(1000, 440)
    return shell
}

const handle = (side: string) =>
    document.querySelector<HTMLElement>(`[data-api-column-handle="${side}"]`)!

describe('clampWidth', () => {
    it('keeps a width inside its limits', () => {
        expect(clampWidth(100, 200, 560)).toBe(200)
        expect(clampWidth(900, 200, 560)).toBe(560)
        expect(clampWidth(300.4, 200, 560)).toBe(300)
    })
})

describe('initApiColumnResize', () => {
    beforeEach(() => {
        localStorage.clear()
        vi.stubGlobal(
            'ResizeObserver',
            class {
                observe() {}
            }
        )
    })

    it('adds a separator for the sidebar and one for the examples rail', () => {
        mountShell()
        initApiColumnResize()

        expect(handle('nav').getAttribute('role')).toBe('separator')
        expect(handle('rail').getAttribute('aria-label')).toBe(
            'Resize examples'
        )
        expect(handle('nav').style.left).toBe('300px')
        expect(handle('rail').style.left).toBe('984px')
    })

    it('grows the sidebar with ArrowRight and the rail with ArrowLeft, and keeps both', () => {
        const shell = mountShell()
        initApiColumnResize()

        handle('nav').dispatchEvent(
            new KeyboardEvent('keydown', { key: 'ArrowRight' })
        )
        handle('rail').dispatchEvent(
            new KeyboardEvent('keydown', { key: 'ArrowLeft' })
        )

        expect(shell.style.getPropertyValue('--layout-nav-width')).toBe('324px')
        expect(shell.style.getPropertyValue('--layout-rail-width')).toBe(
            '464px'
        )
        expect(localStorage.getItem('api-column-widths')).toBe(
            '{"nav":324,"rail":464}'
        )
    })

    it('restores saved widths and a double click gives the column back to the stylesheet', () => {
        localStorage.setItem('api-column-widths', '{"nav":340,"rail":50000}')
        const shell = mountShell()
        initApiColumnResize()

        expect(shell.style.getPropertyValue('--layout-nav-width')).toBe('340px')
        expect(shell.style.getPropertyValue('--layout-rail-width')).toBe(
            '900px'
        )

        handle('rail').dispatchEvent(new MouseEvent('dblclick'))

        expect(shell.style.getPropertyValue('--layout-rail-width')).toBe('')
        expect(localStorage.getItem('api-column-widths')).toBe('{"nav":340}')
    })
})
