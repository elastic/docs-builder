import { initExamplesRailSteadying } from './api-docs'

describe('examples rail steadying', () => {
    let railTop: number
    let animate: jest.Mock
    let frames: FrameRequestCallback[]

    const originalMatchMedia = window.matchMedia

    const setMedia = ({
        desktop,
        reduced,
    }: {
        desktop: boolean
        reduced: boolean
    }) => {
        window.matchMedia = jest.fn((query: string) => ({
            matches: query.includes('reduced-motion') ? reduced : desktop,
        })) as unknown as typeof window.matchMedia
    }

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
        setMedia({ desktop: true, reduced: false })
        frames = []
        jest.spyOn(window, 'requestAnimationFrame').mockImplementation((cb) => {
            frames.push(cb)
            return 0
        })
    })

    afterEach(() => {
        jest.restoreAllMocks()
        window.matchMedia = originalMatchMedia
    })

    // The real handler runs in capture phase, before the toggle mutates the layout
    const clickAndMoveRail = (id: string, newTop: number) => {
        const el = document.getElementById(id)!
        el.addEventListener('click', () => (railTop = newTop), { once: true })
        el.click()
        frames.splice(0).forEach((cb) => cb(0))
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
        setMedia({ desktop: true, reduced: true })

        clickAndMoveRail('toggle', 64)

        expect(animate).not.toHaveBeenCalled()
    })

    it('does not animate below the width where the rail is sticky', () => {
        setMedia({ desktop: false, reduced: false })

        clickAndMoveRail('toggle', 64)

        expect(animate).not.toHaveBeenCalled()
    })

    it('cancels its own previous slide, not other animations on the rail', () => {
        const cancel = jest.fn()
        const rail = document.getElementById('api-examples-panel')!
        rail.animate = jest.fn(() => ({ cancel }) as unknown as Animation)
        rail.getAnimations = jest.fn(() => [])

        clickAndMoveRail('toggle', 64)
        expect(cancel).not.toHaveBeenCalled()

        railTop = 64
        clickAndMoveRail('toggle', 200)

        expect(cancel).toHaveBeenCalledTimes(1)
        expect(rail.getAnimations).not.toHaveBeenCalled()
    })

    it('restarts an interrupted slide from where the rail is drawn', () => {
        const rail = document.getElementById('api-examples-panel')!
        let drawnOffset = 0
        rail.getBoundingClientRect = () =>
            ({ top: railTop + drawnOffset }) as DOMRect
        const slide = jest.fn(() => ({ cancel: () => (drawnOffset = 0) }))
        rail.animate = slide as unknown as typeof rail.animate

        clickAndMoveRail('toggle', 64) // starts a slide from -285

        drawnOffset = -200 // the slide is part-way, so the rail is drawn at -136
        clickAndMoveRail('toggle', 300)

        // The new slide starts at the drawn position: 300 - 436 = -136
        expect(slide).toHaveBeenLastCalledWith(
            [{ transform: 'translateY(-436px)' }, { transform: 'none' }],
            expect.anything()
        )
    })
})
