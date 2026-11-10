import { traceSpan } from './tracing'

type Startable = typeof import('@elastic/opentelemetry-browser')

describe('traceSpan', () => {
    const sent: Array<{ url: string; traceparent: string | null }> = []

    beforeAll(async () => {
        // Record what the SDK's fetch instrumentation sends to the underlying fetch.
        window.fetch = (async (
            input: RequestInfo | URL,
            init?: RequestInit
        ) => {
            const headers = new Headers(init?.headers)
            sent.push({
                url: String(input),
                traceparent: headers.get('traceparent'),
            })
            return new Response('{}')
        }) as typeof fetch

        // The real SDK, so the real context manager and fetch instrumentation run.
        const { startBrowserSdk } = await vi.importActual<Startable>(
            '@elastic/opentelemetry-browser'
        )
        startBrowserSdk({
            serviceName: 'tracing-test',
            exportConfig: {
                tracesUrl: 'http://localhost/t',
                logsUrl: 'http://localhost/l',
                metricsUrl: 'http://localhost/m',
            },
        })
    })

    it('puts a fetch started before the first await on the span trace', async () => {
        let traceId = ''

        await traceSpan('outer', async (span) => {
            traceId = span.spanContext().traceId
            await fetch('/started-before-await')
        })

        const request = sent.find((s) =>
            s.url.endsWith('/started-before-await')
        )
        // traceparent is 00-<trace id>-<span id>-<flags>
        expect(request?.traceparent?.split('-')[1]).toBe(traceId)
    })

    it('rethrows errors from the callback', async () => {
        await expect(
            traceSpan('failing', async () => {
                throw new Error('boom')
            })
        ).rejects.toThrow('boom')
    })
})
