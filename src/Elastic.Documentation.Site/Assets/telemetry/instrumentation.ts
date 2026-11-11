/**
 * Elastic EDOT browser SDK initialisation.
 * Sends traces, logs and metrics to the backend OTLP endpoints (/v1/o/t, /v1/o/l, /v1/o/m).
 *
 * This module should be imported once at application startup.
 * All web components will automatically be instrumented once initialised.
 */
import { config as docsConfig } from '../config'
import { waitForWafToken } from '../waf'
import { detectEnvironment } from './environment'
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
                headers: exportHeaders(),
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

const EXPORT_HEADERS = { 'X-Docs-Session': 'active' }

// The WAF answers /v1/o/* requests without a token with a 202 that the exporter counts as a
// success, so every export waits for the token cookie. SDK start doesn't wait.
function exportHeaders(): Record<string, string> {
    if (!docsConfig.wafIntegrationUrl) return EXPORT_HEADERS

    const tokenReady = waitForWafToken()
    const headers = async () => {
        await tokenReady
        return EXPORT_HEADERS
    }
    // EDOT types headers as an object, but its OTLP exporters accept a function.
    return headers as unknown as Record<string, string>
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

export interface OtelConfigOptions {
    serviceName?: string
    serviceVersion?: string
    baseUrl?: string
    debug?: boolean
}
