/**
 * Elastic EDOT browser SDK initialisation.
 * Sends traces, logs and metrics to the backend OTLP endpoints (/v1/o/t, /v1/o/l, /v1/o/m).
 *
 * This module should be imported once at application startup.
 * All web components will automatically be instrumented once initialised.
 */
import { config as docsConfig } from '../config'
import { startBrowserSdk } from '@elastic/opentelemetry-browser'

let isInitialized = false

export function initializeOtel(options: OtelConfigOptions = {}): boolean {
    if (isSyntheticMonitor()) return false
    if (isInitialized) {
        console.warn(
            'OpenTelemetry already initialized. Skipping re-initialization.'
        )
        return false
    }

    isInitialized = true
    try {
        startBrowserSdk({
            serviceName: options.serviceName ?? docsConfig.serviceName,
            serviceVersion: options.serviceVersion ?? '1.0.0',
            logLevel: options.debug ? 'debug' : 'warn',
            exportConfig: {
                ...otlpSignalUrls(),
                headers: { 'X-Docs-Session': 'active' },
            },
            resourceAttributes: {
                'deployment.environment': detectEnvironment(),
                'service.language.name': 'javascript',
            },
            // Resource timing logs one record per script, font and stylesheet, about 50 per page
            // load. All assets come from the docs origin, so the volume is not worth its cost, and
            // the batches grow past what the WAF accepts without a 403.
            disableInstrumentations: ['resource-timing'],
            // The SDK always ignores its own export URLs. This is the API availability
            // check the search feature polls, which would otherwise be traced.
            ignoreFetchUrls: [/_api\/v1\/?$/],
        })
        return true
    } catch (error) {
        console.error('[OTEL] Failed to initialize OpenTelemetry:', error)
        isInitialized = false
        return false
    }
}

// The ALB routes the browser's OTLP paths to the collector, so each signal gets its own
// absolute URL instead of the SDK's default /v1/{traces,logs,metrics}. EDOT validates them with new URL().
function otlpSignalUrls(): {
    tracesUrl: string
    logsUrl: string
    metricsUrl: string
} {
    const basePath = docsConfig.apiBasePath.replace(/\/+$/, '')
    const signalUrl = (path: string) =>
        new URL(`${basePath}/v1/o/${path}`, window.location.origin).href
    return {
        tracesUrl: signalUrl('t'),
        logsUrl: signalUrl('l'),
        metricsUrl: signalUrl('m'),
    }
}

// The backend excludes synthetic traffic via the X-Docs-Synthetic-Monitor header (see
// synthetics.config.ts / TelemetryConstants.SyntheticMonitorHeaderName), but page JS can't
// read its own outgoing request headers. @elastic/synthetics appends this to the browser's
// UA instead (unless a custom userAgent is configured), so we sniff that for the same effect.
function isSyntheticMonitor(): boolean {
    return navigator.userAgent.includes('Elastic/Synthetics')
}

/**
 * Detects the deployment environment from the hostname at runtime.
 *
 * Since the JavaScript is pre-built and bundled into docs-builder CLI,
 * we detect the environment purely from the runtime hostname in the browser.
 */
function detectEnvironment(): string {
    const hostname = window.location.hostname

    switch (hostname) {
        case 'www.elastic.co':
        case 'elastic.co':
            return 'prod'

        case 'staging-website.elastic.co':
            return 'staging'

        case 'd34ipnu52o64md.cloudfront.net':
            return 'edge'

        case 'localhost':
        case '127.0.0.1':
            return 'local'

        default:
            return 'unknown'
    }
}

export interface OtelConfigOptions {
    serviceName?: string
    serviceVersion?: string
    baseUrl?: string
    debug?: boolean
}
