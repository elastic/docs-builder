import { config } from './config'
import { waitForWafSdk } from './waf'

const integration = {
    fetch: vi.fn(),
    getToken: vi.fn().mockResolvedValue('token'),
}

beforeEach(() => {
    vi.useFakeTimers()
    config.wafIntegrationUrl = 'https://waf.example/jsapi.js'
})

afterEach(() => {
    vi.useRealTimers()
    delete config.wafIntegrationUrl
    delete window.AwsWafIntegration
})

// On edge, jsapi.js adds challenge.js, which defines AwsWafIntegration about 400 to 600 ms
// after the page starts loading.
it('resolves once AwsWafIntegration appears', async () => {
    const ready = waitForWafSdk()
    await vi.advanceTimersByTimeAsync(500)
    window.AwsWafIntegration = integration
    await vi.advanceTimersByTimeAsync(25)

    await expect(ready).resolves.toBe(integration)
})

it('rejects when AwsWafIntegration never appears', async () => {
    const ready = waitForWafSdk()
    const rejected = expect(ready).rejects.toThrow('AWS WAF SDK not available')
    await vi.advanceTimersByTimeAsync(5000)

    await rejected
})

it('resolves to null without an SDK URL', async () => {
    delete config.wafIntegrationUrl

    await expect(waitForWafSdk()).resolves.toBeNull()
})
