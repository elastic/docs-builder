import { initExamplesRailSteadying } from './api-docs'

describe('examples rail steadying', () => {
    let railTop: number
    let animate: jest.Mock
    let frames: FrameRequestCallback[]

    beforeAll(() => initExamplesRailSteadying())

    beforeEach(() => {
        document.body.innerHTML = `
            <section id="elastic-api-v3"><button id="toggle">toggle</button></section>
            <div id="api-examples-panel"></div>
            <button id="outside">outside</button>
        `
        railTop = -285
        animate = jest.fn()
        const rail = document.getElementById('api-examples-panel')!
        rail.getBoundingClientRect = () => ({ top: railTop }) as DOMRect
        rail.animate = animate
        window.matchMedia = jest.fn().mockReturnValue({ matches: false })
        frames = []
        jest.spyOn(window, 'requestAnimationFrame').mockImplementation((cb) => {
            frames.push(cb)
            return 0
        })
    })

    afterEach(() => jest.restoreAllMocks())

    // The real handler runs in capture phase, before the toggle mutates the layout
    const clickAndMoveRail = (id: string, newTop: number) => {
        const el = document.getElementById(id)!
        el.addEventListener('click', () => (railTop = newTop))
        el.click()
        frames.forEach((cb) => cb(0))
    }

    it('slides the rail from its old position when a toggle moves it', () => {
        clickAndMoveRail('toggle', 64)

        expect(animate).toHaveBeenCalledWith(
            [{ transform: 'translateY(-349px)' }, { transform: 'none' }],
            expect.objectContaining({ duration: 200 })
        )
    })

    it('does not animate when the rail did not move', () => {
        clickAndMoveRail('toggle', -285)

        expect(animate).not.toHaveBeenCalled()
    })

    it('ignores clicks outside the operation view', () => {
        clickAndMoveRail('outside', 64)

        expect(animate).not.toHaveBeenCalled()
    })

    it('does not animate with reduced motion', () => {
        window.matchMedia = jest.fn().mockReturnValue({ matches: true })

        clickAndMoveRail('toggle', 64)

        expect(animate).not.toHaveBeenCalled()
    })
})
