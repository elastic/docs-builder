import { config } from './config'

interface AwsWafIntegration {
    fetch: typeof fetch
    getToken: () => Promise<string>
}

declare global {
    interface Window {
        AwsWafIntegration?: AwsWafIntegration
    }
}

const WAF_SDK_TIMEOUT = 5000

let wafSdkLoad: Promise<AwsWafIntegration> | undefined

// Loads the AWS WAF JS SDK from config.wafIntegrationUrl. Resolves to null when no URL is
// configured, so callers fall back to plain fetch.
export const loadWafSdk = (): Promise<AwsWafIntegration | null> => {
    const url = config.wafIntegrationUrl
    if (!url) return Promise.resolve(null)
    if (window.AwsWafIntegration)
        return Promise.resolve(window.AwsWafIntegration)
    if (wafSdkLoad) return wafSdkLoad

    wafSdkLoad = new Promise<AwsWafIntegration>((resolve, reject) => {
        const script = document.createElement('script')
        const fail = (reason: string) => {
            clearTimeout(timer)
            script.remove()
            reject(new Error(reason))
        }
        const timer = setTimeout(
            () => fail('AWS WAF SDK load timed out'),
            WAF_SDK_TIMEOUT
        )
        script.src = url
        script.defer = true
        script.onload = () => {
            clearTimeout(timer)
            const integration = window.AwsWafIntegration
            if (integration) resolve(integration)
            else fail('AWS WAF SDK loaded without AwsWafIntegration')
        }
        script.onerror = () => fail('AWS WAF SDK failed to load')
        document.head.appendChild(script)
    }).finally(() => {
        wafSdkLoad = undefined
    })
    return wafSdkLoad
}

// Resolves once the SDK has stored the aws-waf-token cookie, or after WAF_SDK_TIMEOUT. Never rejects.
export const waitForWafToken = (): Promise<void> => {
    let timer: ReturnType<typeof setTimeout> | undefined
    const timeout = new Promise<void>((resolve) => {
        timer = setTimeout(resolve, WAF_SDK_TIMEOUT)
    })
    const token = loadWafSdk()
        .then((waf) => waf?.getToken())
        .then(
            () => undefined,
            () => undefined
        )
    return Promise.race([token, timeout]).finally(() => clearTimeout(timer))
}
