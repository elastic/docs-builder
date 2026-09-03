import { applyNavWheelDelta, navWheelDeltaY } from './pages-nav-scroll'

describe('applyNavWheelDelta', () => {
    it('clamps to the top when scrolling up past zero', () => {
        expect(applyNavWheelDelta(10, 400, 200, -40)).toBe(0)
    })

    it('clamps to the bottom when scrolling past the end', () => {
        expect(applyNavWheelDelta(180, 400, 200, 40)).toBe(200)
    })

    it('moves within range', () => {
        expect(applyNavWheelDelta(80, 400, 200, 20)).toBe(100)
    })
})

describe('navWheelDeltaY', () => {
    it('uses pixel deltas as-is', () => {
        expect(
            navWheelDeltaY(
                { deltaY: 24, deltaMode: 0, ctrlKey: false } as WheelEvent,
                200
            )
        ).toBe(24)
    })

    it('converts line mode to pixels', () => {
        expect(
            navWheelDeltaY(
                { deltaY: 3, deltaMode: 1, ctrlKey: false } as WheelEvent,
                200
            )
        ).toBe(48)
    })
})
