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
        let poll: ReturnType<typeof setTimeout> | undefined
        const fail = (reason: string) => {
            clearTimeout(timer)
            clearTimeout(poll)
            script.remove()
            reject(new Error(reason))
        }
        const timer = setTimeout(
            () => fail('AWS WAF SDK load timed out'),
            WAF_SDK_TIMEOUT
        )
        // jsapi.js fires load once it has appended challenge.js, which defines
        // AwsWafIntegration a little later, so wait for the global until the timeout.
        const resolveWhenReady = () => {
            const integration = window.AwsWafIntegration
            if (!integration) {
                poll = setTimeout(resolveWhenReady, 25)
                return
            }
            clearTimeout(timer)
            resolve(integration)
        }
        script.src = url
        script.defer = true
        script.onload = resolveWhenReady
        script.onerror = () => fail('AWS WAF SDK failed to load')
        document.head.appendChild(script)
    }).finally(() => {
        wafSdkLoad = undefined
    })
    return wafSdkLoad
}

// Resolves to true once the SDK has stored the aws-waf-token cookie, or to false when the SDK
// fails or WAF_SDK_TIMEOUT passes first. Never rejects.
export const waitForWafToken = (): Promise<boolean> => {
    let timer: ReturnType<typeof setTimeout> | undefined
    const timeout = new Promise<boolean>((resolve) => {
        timer = setTimeout(() => resolve(false), WAF_SDK_TIMEOUT)
    })
    const token = loadWafSdk()
        .then((waf) => waf?.getToken())
        .then(
            () => true,
            () => false
        )
    return Promise.race([token, timeout]).finally(() => clearTimeout(timer))
}
