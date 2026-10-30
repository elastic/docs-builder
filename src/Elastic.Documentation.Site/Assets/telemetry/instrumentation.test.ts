const startBrowserSdk = vi.hoisted(() => vi.fn(() => ({ shutdown: vi.fn() })))

vi.mock('@elastic/opentelemetry-browser', () => ({ startBrowserSdk }))

describe('initializeOtel', () => {
    let initializeOtel: typeof import('./instrumentation').initializeOtel

    beforeEach(async () => {
        startBrowserSdk.mockClear()
        // Re-import so the module-level initialized flag is reset.
        vi.resetModules()
        ;({ initializeOtel } = await import('./instrumentation'))
    })

    afterEach(() => {
        vi.restoreAllMocks()
    })

    const mockUserAgent = (userAgent: string) =>
        vi.spyOn(navigator, 'userAgent', 'get').mockReturnValue(userAgent)

    it('skips initialization for Elastic Synthetics traffic', () => {
        mockUserAgent('Mozilla/5.0 Chrome/120.0.0.0 Elastic/Synthetics')

        expect(initializeOtel()).toBe(false)
        expect(startBrowserSdk).not.toHaveBeenCalled()
    })

    it('does not read document.cookie during initialization', () => {
        mockUserAgent('Mozilla/5.0 Chrome/120.0.0.0')
        const cookieSpy = vi.spyOn(document, 'cookie', 'get')

        initializeOtel()

        expect(cookieSpy).not.toHaveBeenCalled()
    })

    it('initializes the SDK once for non-synthetic traffic', () => {
        mockUserAgent('Mozilla/5.0 Chrome/120.0.0.0')

        expect(initializeOtel()).toBe(true)
        expect(initializeOtel()).toBe(false)
        expect(startBrowserSdk).toHaveBeenCalledTimes(1)
    })

    it('passes an absolute export URL to the SDK', () => {
        mockUserAgent('Mozilla/5.0 Chrome/120.0.0.0')

        initializeOtel()

        const calls = startBrowserSdk.mock.calls as unknown as Array<
            [{ exportConfig: { url: string } }]
        >
        expect(() => new URL(calls[0][0].exportConfig.url)).not.toThrow()
    })
})
