import { collapsedMiddleCount } from './api-breadcrumbs'

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
