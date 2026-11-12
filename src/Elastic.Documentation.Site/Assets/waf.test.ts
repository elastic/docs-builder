import { config } from './config'
import { loadWafSdk } from './waf'

const sdkUrl = 'https://waf.example/jsapi.js'
const integration = {
    fetch: vi.fn(),
    getToken: vi.fn().mockResolvedValue('token'),
}
const sdkScript = () =>
    document.head.querySelector<HTMLScriptElement>(`script[src="${sdkUrl}"]`)

beforeEach(() => {
    vi.useFakeTimers()
    config.wafIntegrationUrl = sdkUrl
})

afterEach(() => {
    vi.useRealTimers()
    delete config.wafIntegrationUrl
    delete window.AwsWafIntegration
    document.head.querySelectorAll('script').forEach((s) => s.remove())
})

// On edge, jsapi.js fires load once it has appended challenge.js, about 20 to 200 ms before
// challenge.js defines AwsWafIntegration.
it('resolves when AwsWafIntegration appears after the script load event', async () => {
    const load = loadWafSdk()
    sdkScript()!.dispatchEvent(new Event('load'))
    await vi.advanceTimersByTimeAsync(200)
    window.AwsWafIntegration = integration
    await vi.advanceTimersByTimeAsync(100)

    await expect(load).resolves.toBe(integration)
})

it('rejects when AwsWafIntegration never appears', async () => {
    const load = loadWafSdk()
    const rejected = expect(load).rejects.toThrow('AWS WAF SDK load timed out')
    sdkScript()!.dispatchEvent(new Event('load'))
    await vi.advanceTimersByTimeAsync(5000)

    await rejected
    expect(sdkScript()).toBeNull()
})

it('resolves to null and loads nothing without an SDK URL', async () => {
    delete config.wafIntegrationUrl

    await expect(loadWafSdk()).resolves.toBeNull()
    expect(document.head.querySelector('script')).toBeNull()
})
