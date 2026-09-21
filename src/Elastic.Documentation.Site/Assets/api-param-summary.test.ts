import { hiddenCountToFit } from './api-param-summary'

describe('hiddenCountToFit', () => {
    it('returns 0 when everything fits', () => {
        expect(hiddenCountToFit(5, () => false)).toBe(0)
    })

    it('returns 0 when there are no names', () => {
        expect(hiddenCountToFit(0, () => true)).toBe(0)
    })

    it('hides the fewest trailing names that make the line fit', () => {
        const overflows = (hidden: number) => hidden < 3
        expect(hiddenCountToFit(8, overflows)).toBe(3)
    })

    it('hides every name when even one plus the badge overflows', () => {
        expect(hiddenCountToFit(4, () => true)).toBe(4)
    })
})
