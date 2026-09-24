import { collapsedMiddleCount } from './api-breadcrumbs'

describe('initApiBreadcrumbs', () => {
    const retained = new Set<Element>()

    beforeEach(() => {
        retained.clear()
        document.body.replaceChildren()
        global.ResizeObserver = class {
            observe(target: Element) {
                retained.add(target)
            }
            unobserve(target: Element) {
                retained.delete(target)
            }
        } as unknown as typeof ResizeObserver
        jest.resetModules()
    })

    it('releases a toolbar after a page swap removes it', async () => {
        const { initApiBreadcrumbs } = await import('./api-breadcrumbs')
        const first = document.createElement('div')
        first.className = 'api-page-toolbar'
        document.body.append(first)

        initApiBreadcrumbs()
        expect([...retained]).toEqual([first])

        first.remove()
        const second = document.createElement('div')
        second.className = 'api-page-toolbar'
        document.body.append(second)
        initApiBreadcrumbs()

        expect([...retained]).toEqual([second])
    })
})

describe('collapsedMiddleCount', () => {
    it('keeps every middle crumb when the row fits', () => {
        expect(collapsedMiddleCount(400, 40, 80, 16, 16, [70, 60])).toBe(0)
    })

    it('hides the crumb before the current page first', () => {
        expect(collapsedMiddleCount(270, 40, 80, 16, 16, [70, 60])).toBe(1)
    })

    it('keeps collapsing leftward until the first and current fit', () => {
        expect(collapsedMiddleCount(200, 40, 80, 16, 16, [70, 60])).toBe(2)
    })

    it('never counts the first or current crumb as overflow', () => {
        expect(collapsedMiddleCount(10, 40, 80, 16, 16, [70])).toBe(1)
        expect(collapsedMiddleCount(10, 40, 80, 16, 16, [])).toBe(0)
    })
})
