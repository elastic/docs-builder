type StartBrowserSdk =
    typeof import('@elastic/opentelemetry-browser').startBrowserSdk

const startBrowserSdk = vi.hoisted(() =>
    vi.fn<StartBrowserSdk>(
        () =>
            ({
                shutdown: () => Promise.resolve(),
            }) as ReturnType<StartBrowserSdk>
    )
)

vi.mock('@elastic/opentelemetry-browser', () => ({ startBrowserSdk }))

describe('initializeOtel', () => {
    let initializeOtel: typeof import('./instrumentation').initializeOtel
    let config: typeof import('../config').config

    beforeEach(async () => {
        startBrowserSdk.mockClear()
        // Re-import so the module-level initialized flag is reset.
        vi.resetModules()
        ;({ initializeOtel } = await import('./instrumentation'))
        ;({ config } = await import('../config'))
    })

    afterEach(() => {
        delete window.AwsWafIntegration
        document.head.querySelectorAll('script').forEach((s) => s.remove())
    })

    const exportHeaders = () =>
        startBrowserSdk.mock.calls[0][0]?.exportConfig?.headers as unknown as
            Record<string, string> | (() => Promise<Record<string, string>>)
    const exportHeadersFn = () =>
        exportHeaders() as () => Promise<Record<string, string>>

    const mockUserAgent = (userAgent: string) =>
        vi.spyOn(navigator, 'userAgent', 'get').mockReturnValue(userAgent)

    it('skips initialization for Elastic Synthetics traffic', () => {
        mockUserAgent('Mozilla/5.0 Chrome/120.0.0.0 Elastic/Synthetics')

        expect(initializeOtel()).toBe(false)
        expect(startBrowserSdk).not.toHaveBeenCalled()
    })

    it('does not read document.cookie while the real SDK starts', async () => {
        mockUserAgent('Mozilla/5.0 Chrome/120.0.0.0')
        // Run the real SDK, with its instrumentations and session handling, so a cookie
        // read added by EDOT is caught. The module-wide mock would hide it.
        const actual = await vi.importActual<
            typeof import('@elastic/opentelemetry-browser')
        >('@elastic/opentelemetry-browser')
        startBrowserSdk.mockImplementationOnce(actual.startBrowserSdk)
        const fetchBefore = window.fetch
        const cookieSpy = vi.spyOn(document, 'cookie', 'get')

        expect(initializeOtel()).toBe(true)

        // The real fetch instrumentation wraps fetch, so this was not a stub.
        expect(window.fetch).not.toBe(fetchBefore)
        expect(cookieSpy).not.toHaveBeenCalled()
    })

    it('initializes the SDK once for non-synthetic traffic', () => {
        mockUserAgent('Mozilla/5.0 Chrome/120.0.0.0')

        expect(initializeOtel()).toBe(true)
        expect(initializeOtel()).toBe(false)
        expect(startBrowserSdk).toHaveBeenCalledTimes(1)
    })

    it('exports each signal to its /v1/o path under the API base path', () => {
        mockUserAgent('Mozilla/5.0 Chrome/120.0.0.0')

        initializeOtel()

        const calls = startBrowserSdk.mock.calls as unknown as Array<
            [
                {
                    exportConfig: {
                        tracesUrl: string
                        logsUrl: string
                        metricsUrl: string
                    }
                },
            ]
        >
        const { tracesUrl, logsUrl, metricsUrl } = calls[0][0].exportConfig
        const { origin } = window.location
        expect(tracesUrl).toBe(`${origin}/docs/_api/v1/o/t`)
        expect(logsUrl).toBe(`${origin}/docs/_api/v1/o/l`)
        expect(metricsUrl).toBe(`${origin}/docs/_api/v1/o/m`)
    })

    it('turns off resource timing, which emits a log record per asset', () => {
        mockUserAgent('Mozilla/5.0 Chrome/120.0.0.0')

        initializeOtel()

        expect(
            startBrowserSdk.mock.calls[0][0]?.disableInstrumentations
        ).toEqual(['resource-timing'])
    })

    describe('AWS WAF token', () => {
        const sdkUrl = 'https://waf.example/challenge.js'

        beforeEach(() => {
            mockUserAgent('Mozilla/5.0 Chrome/120.0.0.0')
        })

        it('sends plain headers and loads no script without an SDK URL', () => {
            initializeOtel()

            expect(exportHeaders()).toEqual({ 'X-Docs-Session': 'active' })
            expect(document.head.querySelector('script')).toBeNull()
        })

        it('holds each export until the WAF token is fetched', async () => {
            config.wafIntegrationUrl = sdkUrl
            let issueToken: (token: string) => void = () => {}
            window.AwsWafIntegration = {
                fetch: vi.fn(),
                getToken: () =>
                    new Promise<string>((resolve) => (issueToken = resolve)),
            }

            initializeOtel()
            const headers = exportHeadersFn()
            expect(
                await Promise.race([headers(), Promise.resolve('pending')])
            ).toBe('pending')

            issueToken('token')

            expect(await headers()).toEqual({ 'X-Docs-Session': 'active' })
        })

        it('still exports when the WAF SDK fails to load', async () => {
            config.wafIntegrationUrl = sdkUrl

            initializeOtel()
            const headers = exportHeadersFn()
            document.head
                .querySelector(`script[src="${sdkUrl}"]`)!
                .dispatchEvent(new Event('error'))

            expect(await headers()).toEqual({ 'X-Docs-Session': 'active' })
        })
    })
})
