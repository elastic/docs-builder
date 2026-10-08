import { flashTooltip } from './tooltip'

describe('flashTooltip', () => {
    beforeEach(() => jest.useFakeTimers())
    afterEach(() => jest.useRealTimers())

    it('swaps the text and restores it after the delay', () => {
        const el = document.createElement('button')
        el.dataset.tippyContent = 'Copy'
        flashTooltip(el, 'Copied!')
        expect(el.dataset.tippyContent).toBe('Copied!')
        jest.advanceTimersByTime(1500)
        expect(el.dataset.tippyContent).toBe('Copy')
    })

    it('keeps the original text across repeated flashes', () => {
        const el = document.createElement('button')
        el.dataset.tippyContent = 'Copy'
        flashTooltip(el, 'Copied!')
        flashTooltip(el, 'Copied!')
        jest.advanceTimersByTime(1500)
        expect(el.dataset.tippyContent).toBe('Copy')
    })

    it('restarts the delay on a repeated flash', () => {
        const el = document.createElement('button')
        el.dataset.tippyContent = 'Copy'
        flashTooltip(el, 'Copied!')
        jest.advanceTimersByTime(1000)
        flashTooltip(el, 'Copied!')
        jest.advanceTimersByTime(1000)
        expect(el.dataset.tippyContent).toBe('Copied!')
        jest.advanceTimersByTime(500)
        expect(el.dataset.tippyContent).toBe('Copy')
    })
})
