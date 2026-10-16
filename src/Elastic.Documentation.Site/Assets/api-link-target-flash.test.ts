function markup(): string {
    return `<section id="elastic-api-v3"><dl><dt id="prop">rule</dt></dl></section>`
}

const observers: {
    cb: (entries: { isIntersecting: boolean }[]) => void
}[] = []

/** Reports the target on screen and lets the settle delay pass. */
function scrollIntoView(visible = true) {
    ;[...observers].forEach((o) => o.cb([{ isIntersecting: visible }]))
    jest.advanceTimersByTime(250)
}

async function open(navType: string, hash: string) {
    document.body.innerHTML = markup()
    window.location.hash = hash
    Object.defineProperty(performance, 'getEntriesByType', {
        configurable: true,
        value: () => [{ type: navType }],
    })
    observers.length = 0
    window.IntersectionObserver = class {
        constructor(
            public cb: (entries: { isIntersecting: boolean }[]) => void
        ) {
            observers.push(this)
        }
        observe() {}
        disconnect() {
            observers.splice(observers.indexOf(this), 1)
        }
    } as unknown as typeof IntersectionObserver
    let api!: typeof import('./api-docs')
    await jest.isolateModulesAsync(async () => {
        api = await import('./api-docs')
    })
    return api
}

const prop = () => document.getElementById('prop')!

describe('flash of the linked target', () => {
    beforeEach(() => jest.useFakeTimers())

    afterEach(() => {
        jest.useRealTimers()
        jest.restoreAllMocks()
        window.location.hash = ''
    })

    it.each(['navigate', 'reload'])(
        'flashes on %s with a fragment',
        async (type) => {
            ;(await open(type, '#prop')).initApiDocs()
            scrollIntoView()
            expect(prop().classList.contains('api-link-target-flash')).toBe(
                true
            )
        }
    )

    it('stays quiet on back/forward', async () => {
        ;(await open('back_forward', '#prop')).initApiDocs()
        expect(prop().classList.contains('api-link-target-flash')).toBe(false)
    })

    it('stays quiet without a fragment', async () => {
        ;(await open('navigate', '')).initApiDocs()
        expect(prop().classList.contains('api-link-target-flash')).toBe(false)
    })

    it('flashes only on the first init, not on an htmx swap', async () => {
        const api = await open('navigate', '#prop')
        api.initApiDocs()
        scrollIntoView()
        prop().classList.remove('api-link-target-flash')
        api.initApiDocs()
        scrollIntoView()
        expect(prop().classList.contains('api-link-target-flash')).toBe(false)
    })

    it('waits until the target is on screen', async () => {
        ;(await open('navigate', '#prop')).initApiDocs()
        jest.advanceTimersByTime(1000)
        expect(prop().classList.contains('api-link-target-flash')).toBe(false)
        scrollIntoView()
        expect(prop().classList.contains('api-link-target-flash')).toBe(true)
    })

    it('does not flash a target that only passed through the viewport', async () => {
        ;(await open('navigate', '#prop')).initApiDocs()
        observers.forEach((o) => o.cb([{ isIntersecting: true }]))
        jest.advanceTimersByTime(100)
        scrollIntoView(false)
        expect(prop().classList.contains('api-link-target-flash')).toBe(false)
    })

    it('stays quiet on a later hashchange', async () => {
        const api = await open('navigate', '')
        api.initApiDocs()
        window.location.hash = '#prop'
        window.dispatchEvent(new HashChangeEvent('hashchange'))
        expect(prop().classList.contains('api-link-target-flash')).toBe(false)
    })

    it('clears the class when the animation ends', async () => {
        ;(await open('navigate', '#prop')).initApiDocs()
        scrollIntoView()
        prop().dispatchEvent(new Event('animationend'))
        expect(prop().classList.contains('api-link-target-flash')).toBe(false)
    })
})
