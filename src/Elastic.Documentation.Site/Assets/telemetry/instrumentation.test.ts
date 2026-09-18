import { initializeOtel } from './instrumentation'

describe('initializeOtel', () => {
    it('skips initialization for Elastic Synthetics traffic', () => {
        jest.spyOn(navigator, 'userAgent', 'get').mockReturnValue(
            'Mozilla/5.0 Chrome/120.0.0.0 Elastic/Synthetics'
        )

        expect(initializeOtel()).toBe(false)
    })

    it('does not read document.cookie during initialization', () => {
        const cookieSpy = jest.spyOn(document, 'cookie', 'get')

        initializeOtel()

        expect(cookieSpy).not.toHaveBeenCalled()
    })
})
