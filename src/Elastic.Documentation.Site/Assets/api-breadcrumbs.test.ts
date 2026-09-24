import { collapsedMiddleCount } from './api-breadcrumbs'

const originalResizeObserver = global.ResizeObserver

afterEach(() => {
    global.ResizeObserver = originalResizeObserver
    document.body.innerHTML = ''
})

function installTrackingResizeObserver() {
    const targets = new Set<Element>()
    const observes = new Map<Element, number>()

    class TrackingResizeObserver {
        observe(target: Element) {
            targets.add(target)
            observes.set(target, (observes.get(target) ?? 0) + 1)
        }

        unobserve(target: Element) {
            targets.delete(target)
        }

        disconnect() {
            targets.clear()
        }
    }

    global.ResizeObserver =
        TrackingResizeObserver as unknown as typeof ResizeObserver
    return { targets, observes }
}

async function loadBreadcrumbs() {
    jest.resetModules()
    const tracking = installTrackingResizeObserver()
    const mod = await import('./api-breadcrumbs')
    return { initApiBreadcrumbs: mod.initApiBreadcrumbs, ...tracking }
}

function toolbar(id: string) {
    return `<div class="api-page-toolbar" id="${id}"></div>`
}

describe('initApiBreadcrumbs', () => {
    it('releases toolbars removed by a page swap and observes the new one once', async () => {
        const { initApiBreadcrumbs, targets, observes } =
            await loadBreadcrumbs()

        document.body.innerHTML = toolbar('first')
        const first = document.getElementById('first')!
        initApiBreadcrumbs()
        initApiBreadcrumbs()

        first.remove()
        document.body.innerHTML = toolbar('second')
        const second = document.getElementById('second')!
        initApiBreadcrumbs()

        expect([...targets]).toEqual([second])
        expect(observes.get(first)).toBe(1)
        expect(observes.get(second)).toBe(1)
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
