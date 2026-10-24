const FLASH = 'api-link-target-flash'

const MARKUP = `
    <ul id="paths"><li><a id="param-link" class="api-url-param" href="#prop">{rule}</a></li></ul>
    <section id="elastic-api-v3">
        <dl><dt id="prop"><code id="prop-name">rule</code></dt></dl>
        <h2 id="heading">Query parameters</h2>
    </section>`

const observers: {
    cb: (entries: { isIntersecting: boolean }[]) => void
}[] = []

function report(visible: boolean) {
    ;[...observers].forEach((o) => o.cb([{ isIntersecting: visible }]))
}

/** Reports the target on screen and lets the settle delay pass. */
function scrollIntoView(visible = true) {
    report(visible)
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
    document.body.innerHTML = MARKUP
    window.location.hash = hash
    // jsdom reports the hash change on a timer; run it before the page's listener exists
    jest.runOnlyPendingTimers()
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

const flashing = (id: string) =>
    document.getElementById(id)!.classList.contains(FLASH)

Element.prototype.scrollIntoView = jest.fn()

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
        expect(flashing('prop-name')).toBe(true)
    })

    it('pulses the name of a property row, not the row', async () => {
        ;(await open('#prop')).initApiDocs()
        scrollIntoView()
        expect(flashing('prop-name')).toBe(true)
        expect(flashing('prop')).toBe(false)
    })

    it('flashes the target when the fragment changes', async () => {
        ;(await open('')).initApiDocs()
        window.location.hash = '#prop'
        window.dispatchEvent(new HashChangeEvent('hashchange'))
        scrollIntoView()
        expect(flashing('prop-name')).toBe(true)
    })

    it('flashes the target of an in-page link', async () => {
        ;(await open('')).initApiDocs()
        document.getElementById('param-link')!.click()
        scrollIntoView()
        expect(flashing('prop-name')).toBe(true)
    })

    it('stays quiet without a fragment', async () => {
        ;(await open('')).initApiDocs()
        scrollIntoView()
        expect(flashing('prop-name')).toBe(false)
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
        document.getElementById('prop-name')!.classList.remove(FLASH)
        api.initApiDocs()
        scrollIntoView()
        expect(flashing('prop-name')).toBe(false)
    })

    it('flashes a property reached by an htmx search navigation', async () => {
        const api = await open('')
        api.initApiDocs()
        document.body.innerHTML = MARKUP
        history.pushState(null, '', '#prop')
        api.initApiDocs()
        scrollIntoView()
        expect(flashing('prop-name')).toBe(true)
    })

    it('flashes the same fragment again when navigation replaces the page', async () => {
        const api = await open('#prop')
        api.initApiDocs()
        scrollIntoView()
        document.body.innerHTML = MARKUP
        api.initApiDocs()
        scrollIntoView()
        expect(flashing('prop-name')).toBe(true)
    })

    it('waits until the target is on screen', async () => {
        ;(await open('#prop')).initApiDocs()
        jest.advanceTimersByTime(1000)
        expect(flashing('prop-name')).toBe(false)
        scrollIntoView()
        expect(flashing('prop-name')).toBe(true)
    })

    it('does not flash a target that only passed through the viewport', async () => {
        ;(await open('#prop')).initApiDocs()
        report(true)
        jest.advanceTimersByTime(100)
        scrollIntoView(false)
        expect(flashing('prop-name')).toBe(false)
    })

    it('keeps a second flash on the same target alive past the timer of the first', async () => {
        ;(await open('#prop')).initApiDocs()
        scrollIntoView()
        // The first flash started at 200ms and its fallback fires at 3400ms
        jest.advanceTimersByTime(2000)
        window.location.hash = '#other'
        window.location.hash = '#prop'
        window.dispatchEvent(new HashChangeEvent('hashchange'))
        scrollIntoView()
        // Past the first flash's deadline, while the second one is still inside its own
        jest.advanceTimersByTime(1500)
        expect(flashing('prop-name')).toBe(true)
    })

    it('clears the class when the animation ends', async () => {
        ;(await open('#prop')).initApiDocs()
        scrollIntoView()
        document
            .getElementById('prop-name')!
            .dispatchEvent(new Event('animationend'))
        expect(flashing('prop-name')).toBe(false)
    })
})
