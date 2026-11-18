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

// Layout/_Head.cshtml loads the AWS WAF SDK when wafIntegrationUrl is set. Its jsapi.js adds
// challenge.js, which defines AwsWafIntegration a little after the page loads, so this waits
// for the global. Resolves to null without a URL, so callers fall back to plain fetch.
export const waitForWafSdk = (): Promise<AwsWafIntegration | null> => {
    if (!config.wafIntegrationUrl) return Promise.resolve(null)

    const deadline = Date.now() + WAF_SDK_TIMEOUT
    return new Promise((resolve, reject) => {
        const check = () => {
            if (window.AwsWafIntegration) resolve(window.AwsWafIntegration)
            else if (Date.now() >= deadline)
                reject(new Error('AWS WAF SDK not available'))
            else setTimeout(check, 25)
        }
        check()
    })
}

// Resolves to true once the SDK has stored the aws-waf-token cookie, or to false when the SDK
// fails or WAF_SDK_TIMEOUT passes first. Never rejects.
export const waitForWafToken = (): Promise<boolean> => {
    let timer: ReturnType<typeof setTimeout> | undefined
    const timeout = new Promise<boolean>((resolve) => {
        timer = setTimeout(() => resolve(false), WAF_SDK_TIMEOUT)
    })
    const token = waitForWafSdk()
        .then((waf) => waf?.getToken())
        .then(
            () => true,
            () => false
        )
    return Promise.race([token, timeout]).finally(() => clearTimeout(timer))
}
