import { balanceCodeHeights } from './api-examples-balance'

const floor = 112

describe('balanceCodeHeights', () => {
    it('shows both in full when they fit', () => {
        expect(
            balanceCodeHeights(500, { request: 200, response: 100 }, floor)
        ).toEqual({ request: 200, response: 100 })
    })

    it('gives a one-line response its full height and the rest to the request', () => {
        expect(
            balanceCodeHeights(300, { request: 360, response: 24 }, floor)
        ).toEqual({ request: 276, response: 24 })
    })

    it('gives a short request its full height and the rest to the response', () => {
        expect(
            balanceCodeHeights(400, { request: 120, response: 4644 }, floor)
        ).toEqual({ request: 120, response: 280 })
    })

    it('splits the room evenly between two long samples', () => {
        expect(
            balanceCodeHeights(400, { request: 360, response: 4644 }, floor)
        ).toEqual({ request: 200, response: 200 })
    })

    it('keeps five lines of each when the window is too short', () => {
        expect(
            balanceCodeHeights(-50, { request: 360, response: 4644 }, floor)
        ).toEqual({ request: 112, response: 112 })
    })

    it('never grows a sample past its own height to reach the floor', () => {
        expect(
            balanceCodeHeights(0, { request: 360, response: 42 }, floor)
        ).toEqual({ request: 112, response: 42 })
    })
})
