import { clampWidth, initApiColumnResize } from './api-column-resize'

const rect = (left: number, width: number) =>
    ({ left, right: left + width, width }) as DOMRect

// The columns report the widths the script applied through its custom properties, as the real layout does. The rail's right edge
// stays where it is, so growing it moves its left edge.
function mountShell({ width = 1600, nav = 300, rail = 640 } = {}) {
    document.body.innerHTML = `<div class="api-page-shell layout--three">
        <aside class="layout__nav"></aside>
        <aside class="api-page-rail"></aside>
    </div>`
    const shell = document.querySelector<HTMLElement>('.api-page-shell')!
    const applied = (name: string, fallback: number) =>
        parseFloat(shell.style.getPropertyValue(name)) || fallback
    shell.getBoundingClientRect = () => rect(0, width)
    document.querySelector('.layout__nav')!.getBoundingClientRect = () =>
        rect(0, applied('--layout-nav-width', nav))
    document.querySelector('.api-page-rail')!.getBoundingClientRect = () => {
        const current = applied('--layout-rail-width', rail)
        return rect(1000 + rail - current, current)
    }
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
        expect(handle('nav').style.left).toBe('303px')
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
            '664px'
        )
        expect(localStorage.getItem('api-column-widths')).toBe(
            '{"nav":324,"rail":664}'
        )
    })

    it('restores saved widths and a double click gives the column back to the stylesheet', () => {
        localStorage.setItem('api-column-widths', '{"nav":340,"rail":50000}')
        const shell = mountShell({ width: 2000 })
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

    it('lets go of the shell when navigation leaves the API pages', () => {
        const remove = vi.spyOn(window, 'removeEventListener')
        mountShell()
        initApiColumnResize()
        disconnects = 0
        document.body.innerHTML = '<p>A documentation page</p>'
        initApiColumnResize()

        expect(disconnects).toBe(1)
        expect(remove).toHaveBeenCalledWith('resize', expect.any(Function))
        remove.mockRestore()
    })

    it('shrinks saved widths, rail first, when the window is too narrow for them', () => {
        localStorage.setItem('api-column-widths', '{"nav":560,"rail":900}')
        const shell = mountShell({ width: 1000 })
        initApiColumnResize()

        expect(shell.style.getPropertyValue('--layout-rail-width')).toBe(
            '320px'
        )
        expect(shell.style.getPropertyValue('--layout-nav-width')).toBe('256px')
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

    it('stops a key step at the width that keeps the reference at its minimum, across both columns', () => {
        const shell = mountShell({ width: 1380 })
        initApiColumnResize()

        handle('rail').dispatchEvent(
            new KeyboardEvent('keydown', { key: 'ArrowLeft' })
        )
        handle('nav').dispatchEvent(
            new KeyboardEvent('keydown', { key: 'ArrowRight' })
        )

        expect(shell.style.getPropertyValue('--layout-rail-width')).toBe(
            '656px'
        )
        expect(shell.style.getPropertyValue('--layout-nav-width')).toBe('300px')
    })

    it('stops a drag at the width that keeps the reference at its minimum', () => {
        const shell = mountShell()
        initApiColumnResize()

        handle('rail').dispatchEvent(
            new MouseEvent('pointerdown', { clientX: 2000 })
        )
        handle('rail').dispatchEvent(
            new MouseEvent('pointermove', { clientX: 0 })
        )
        handle('rail').dispatchEvent(new MouseEvent('pointerup'))

        expect(shell.style.getPropertyValue('--layout-rail-width')).toBe(
            '876px'
        )
    })

    it('follows a drag and saves the width only when the drag ends', () => {
        const shell = mountShell()
        initApiColumnResize()

        handle('rail').dispatchEvent(
            new MouseEvent('pointerdown', { clientX: 100 })
        )
        handle('rail').dispatchEvent(
            new MouseEvent('pointermove', { clientX: 60 })
        )

        expect(shell.style.getPropertyValue('--layout-rail-width')).toBe(
            '680px'
        )
        expect(handle('rail').dataset.dragging).toBe('')
        expect(localStorage.getItem('api-column-widths')).toBeNull()

        handle('rail').dispatchEvent(new MouseEvent('pointerup'))

        expect(handle('rail').dataset.dragging).toBeUndefined()
        expect(localStorage.getItem('api-column-widths')).toBe('{"rail":680}')
    })

    it('does not move a sidebar that sits at the design-system floor when a drag starts', () => {
        const shell = mountShell({ nav: 224 })
        initApiColumnResize()

        handle('nav').dispatchEvent(
            new MouseEvent('pointerdown', { clientX: 100 })
        )
        handle('nav').dispatchEvent(
            new MouseEvent('pointermove', { clientX: 100 })
        )

        expect(shell.style.getPropertyValue('--layout-nav-width')).toBe('224px')
    })

    it('does not drag the examples rail under 320px', () => {
        const shell = mountShell()
        initApiColumnResize()

        handle('rail').dispatchEvent(
            new MouseEvent('pointerdown', { clientX: 100 })
        )
        handle('rail').dispatchEvent(
            new MouseEvent('pointermove', { clientX: 1500 })
        )

        expect(shell.style.getPropertyValue('--layout-rail-width')).toBe(
            '320px'
        )
    })
})
