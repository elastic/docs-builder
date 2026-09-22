const mockStartBrowserSdk = jest.fn(() => ({ forceFlush: jest.fn() }))

jest.mock('@elastic/opentelemetry-browser', () => ({
    startBrowserSdk: mockStartBrowserSdk,
}))

describe('initializeOtel', () => {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    let initializeOtel: (options?: any) => boolean

    beforeEach(() => {
        mockStartBrowserSdk.mockClear()
        mockStartBrowserSdk.mockReturnValue({ forceFlush: jest.fn() })
        jest.resetModules()
        // Re-require after resetModules so the module-level `sdk` variable is reset.
        // require() is available in Jest's runtime; cast avoids missing node types in tsconfig.
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        initializeOtel = (require as any)('./instrumentation').initializeOtel
    })

    afterEach(() => {
        jest.restoreAllMocks()
    })

    it('skips initialization for Elastic Synthetics traffic', () => {
        jest.spyOn(navigator, 'userAgent', 'get').mockReturnValue(
            'Mozilla/5.0 Chrome/120.0.0.0 Elastic/Synthetics'
        )

        expect(initializeOtel()).toBe(false)
        expect(mockStartBrowserSdk).not.toHaveBeenCalled()
    })

    it('does not read document.cookie during initialization', () => {
        jest.spyOn(navigator, 'userAgent', 'get').mockReturnValue(
            'Mozilla/5.0 Chrome/120.0.0.0'
        )
        const cookieSpy = jest.spyOn(document, 'cookie', 'get')

        initializeOtel()

        expect(cookieSpy).not.toHaveBeenCalled()
    })

    it('initializes the SDK for non-synthetic traffic', () => {
        jest.spyOn(navigator, 'userAgent', 'get').mockReturnValue(
            'Mozilla/5.0 Chrome/120.0.0.0'
        )

        expect(initializeOtel()).toBe(true)
        expect(mockStartBrowserSdk).toHaveBeenCalledTimes(1)
    })

    it('skips re-initialization when called twice', () => {
        jest.spyOn(navigator, 'userAgent', 'get').mockReturnValue(
            'Mozilla/5.0 Chrome/120.0.0.0'
        )

        initializeOtel()
        const result = initializeOtel()

        expect(result).toBe(false)
        expect(mockStartBrowserSdk).toHaveBeenCalledTimes(1)
    })
})
