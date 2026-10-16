const FLASH = 'api-link-target-flash'

function markup(): string {
    return `
        <ul id="paths"><li><a id="param-link" class="api-url-param" href="#prop">{rule}</a></li></ul>
        <section id="elastic-api-v3">
            <dl><dt id="prop">rule</dt></dl>
            <h2 id="heading">Query parameters</h2>
        </section>`
}

const observers: {
    cb: (entries: { isIntersecting: boolean }[]) => void
}[] = []

/** Reports the target on screen and lets the settle delay pass. */
function scrollIntoView(visible = true) {
    ;[...observers].forEach((o) => o.cb([{ isIntersecting: visible }]))
    jest.advanceTimersByTime(250)
}

const hashListeners: EventListener[] = []
const addListener = window.addEventListener.bind(window)
window.addEventListener = ((
    type: string,
    listener: EventListener,
    ...rest: []
) => {
    if (type === 'hashchange') hashListeners.push(listener)
    addListener(type, listener, ...rest)
}) as typeof window.addEventListener

async function open(hash: string) {
    document.body.innerHTML = markup()
    window.location.hash = hash
    // jsdom reports the hash change on a timer; run it before the page's listener exists
    jest.runOnlyPendingTimers()
    observers.length = 0
    Element.prototype.scrollIntoView = jest.fn()
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

const flashing = (id: string) =>
    document.getElementById(id)!.classList.contains(FLASH)

describe('flash of the linked target', () => {
    beforeEach(() => jest.useFakeTimers())

    afterEach(() => {
        hashListeners
            .splice(0)
            .forEach((l) => window.removeEventListener('hashchange', l))
        jest.useRealTimers()
        window.location.hash = ''
    })

    it('flashes the target when the page opens with a fragment', async () => {
        ;(await open('#prop')).initApiDocs()
        scrollIntoView()
        expect(flashing('prop')).toBe(true)
    })

    it('flashes the target when the fragment changes', async () => {
        ;(await open('')).initApiDocs()
        window.location.hash = '#prop'
        window.dispatchEvent(new HashChangeEvent('hashchange'))
        scrollIntoView()
        expect(flashing('prop')).toBe(true)
    })

    it('flashes the target of an in-page link', async () => {
        ;(await open('')).initApiDocs()
        document.getElementById('param-link')!.click()
        scrollIntoView()
        expect(flashing('prop')).toBe(true)
    })

    it('stays quiet without a fragment', async () => {
        ;(await open('')).initApiDocs()
        scrollIntoView()
        expect(flashing('prop')).toBe(false)
    })

    it('stays quiet when the fragment points at a heading', async () => {
        ;(await open('#heading')).initApiDocs()
        scrollIntoView()
        expect(flashing('heading')).toBe(false)
    })

    it('does not replay the opening flash on an htmx swap', async () => {
        const api = await open('#prop')
        api.initApiDocs()
        scrollIntoView()
        document.getElementById('prop')!.classList.remove(FLASH)
        api.initApiDocs()
        scrollIntoView()
        expect(flashing('prop')).toBe(false)
    })

    it('waits until the target is on screen', async () => {
        ;(await open('#prop')).initApiDocs()
        jest.advanceTimersByTime(1000)
        expect(flashing('prop')).toBe(false)
        scrollIntoView()
        expect(flashing('prop')).toBe(true)
    })

    it('does not flash a target that only passed through the viewport', async () => {
        ;(await open('#prop')).initApiDocs()
        ;[...observers].forEach((o) => o.cb([{ isIntersecting: true }]))
        jest.advanceTimersByTime(100)
        scrollIntoView(false)
        expect(flashing('prop')).toBe(false)
    })

    it('keeps a second flash on the same target alive past the timer of the first', async () => {
        ;(await open('#prop')).initApiDocs()
        scrollIntoView()
        jest.advanceTimersByTime(3000)
        window.location.hash = '#other'
        window.location.hash = '#prop'
        window.dispatchEvent(new HashChangeEvent('hashchange'))
        scrollIntoView()
        jest.advanceTimersByTime(300)
        expect(flashing('prop')).toBe(true)
    })

    it('clears the class when the animation ends', async () => {
        ;(await open('#prop')).initApiDocs()
        scrollIntoView()
        document
            .getElementById('prop')!
            .dispatchEvent(new Event('animationend'))
        expect(flashing('prop')).toBe(false)
    })
})
