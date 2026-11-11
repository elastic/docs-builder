import { clampWidth, initApiColumnResize } from './api-column-resize'

const rect = (left: number, width: number) =>
    ({ left, right: left + width, width }) as DOMRect

function mountShell(width = 1600) {
    document.body.innerHTML = `<div class="api-page-shell layout--three">
        <aside class="layout__nav"></aside>
        <aside class="api-page-rail"></aside>
    </div>`
    const shell = document.querySelector<HTMLElement>('.api-page-shell')!
    shell.getBoundingClientRect = () => rect(0, width)
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

let disconnects = 0

describe('initApiColumnResize', () => {
    beforeEach(() => {
        vi.unstubAllGlobals()
        disconnects = 0
        localStorage.clear()
        vi.stubGlobal(
            'ResizeObserver',
            class {
                observe() {}
                disconnect() {
                    disconnects++
                }
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
        expect(handle('nav').style.left).toBe('306px')
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

    it('lets go of the replaced shell when a new one is mounted', () => {
        const remove = vi.spyOn(window, 'removeEventListener')
        mountShell()
        initApiColumnResize()
        disconnects = 0
        mountShell()
        initApiColumnResize()

        expect(disconnects).toBe(1)
        expect(remove).toHaveBeenCalledWith('resize', expect.any(Function))
        remove.mockRestore()
    })

    it('shrinks saved widths, rail first, when the window is too narrow for them', () => {
        localStorage.setItem('api-column-widths', '{"nav":560,"rail":900}')
        const shell = mountShell(1000)
        initApiColumnResize()

        expect(shell.style.getPropertyValue('--layout-rail-width')).toBe(
            '280px'
        )
        expect(shell.style.getPropertyValue('--layout-nav-width')).toBe('296px')
        expect(localStorage.getItem('api-column-widths')).toBe(
            '{"nav":560,"rail":900}'
        )
    })

    it('leaves the stylesheet widths alone below the breakpoint that shows the handles', () => {
        vi.stubGlobal('matchMedia', () => ({ matches: false }))
        localStorage.setItem('api-column-widths', '{"nav":560,"rail":900}')
        const shell = mountShell()
        initApiColumnResize()

        expect(shell.style.getPropertyValue('--layout-nav-width')).toBe('')
        expect(shell.style.getPropertyValue('--layout-rail-width')).toBe('')
    })
})
