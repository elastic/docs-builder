/**
 * @vitest-environment-options {"url": "http://localhost:47811/docs/"}
 */
// Runs the real EDOT SDK and OTLP log exporter, unlike instrumentation.test.ts. The async headers
// function reaches the exporter only through a cast, so an SDK upgrade could drop it. Vitest
// resolves the exporter's Node transport, so a local server stands in for the collector, and
// the test flushes the provider because the Node batch processor doesn't flush on pagehide.
import { config } from '../config'
import { initializeOtel } from './instrumentation'
import { logs } from '@opentelemetry/api-logs'
import { createServer, type IncomingHttpHeaders } from 'node:http'

const collector = {
    requests: [] as { path: string; headers: IncomingHttpHeaders }[],
}
const server = createServer((req, res) => {
    collector.requests.push({ path: req.url ?? '', headers: req.headers })
    req.resume()
    res.writeHead(200).end()
})

beforeAll(() => new Promise<void>((resolve) => server.listen(47811, resolve)))
afterAll(() => new Promise<void>((resolve) => server.close(() => resolve())))

it('the real log exporter waits for the WAF token before it sends', async () => {
    vi.spyOn(navigator, 'userAgent', 'get').mockReturnValue(
        'Mozilla/5.0 Chrome/120.0.0.0'
    )
    config.wafIntegrationUrl = 'https://waf.example/challenge.js'
    let issueToken: (token: string) => void = () => {}
    window.AwsWafIntegration = {
        fetch: vi.fn(),
        getToken: () =>
            new Promise<string>((resolve) => (issueToken = resolve)),
    }
    const logExports = () =>
        collector.requests.filter((r) => r.path.endsWith('/v1/o/l'))

    expect(initializeOtel()).toBe(true)
    logs.getLogger('waf-test').emit({ body: 'hello' })
    const flushed = (
        logs.getLoggerProvider() as unknown as { forceFlush(): Promise<void> }
    ).forceFlush()
    await new Promise((resolve) => setTimeout(resolve, 200))
    expect(logExports()).toHaveLength(0)

    issueToken('token')

    await flushed
    expect(logExports()).toHaveLength(1)
    expect(logExports()[0].headers['x-docs-session']).toBe('active')
})
