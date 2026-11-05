/**
 * Elastic EDOT browser SDK initialisation.
 * Sends traces and logs to the backend OTLP endpoints (/v1/traces, /v1/logs).
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
                url: otlpBaseUrl(),
                headers: { 'X-Docs-Session': 'active' },
            },
            resourceAttributes: {
                'deployment.environment': detectEnvironment(),
                'service.language.name': 'javascript',
            },
            instrumentations: {
                fetch: {
                    propagateTraceHeaderCorsUrls: [
                        new RegExp(`${window.location.origin}/.*`),
                    ],
                    // Avoid recursive telemetry: ignore the OTLP endpoints and
                    // the root API availability check used by the search feature.
                    ignoreUrls: [
                        /\/v1\/(traces|logs|metrics)$/,
                        /_api\/v1\/?$/,
                    ],
                },
            },
        })
        return true
    } catch (error) {
        console.error('[OTEL] Failed to initialize OpenTelemetry:', error)
        isInitialized = false
        return false
    }
}

// EDOT validates this with new URL() and appends v1/traces, v1/logs and v1/metrics by plain
// concatenation, so the path needs a trailing slash: /docs/_api/ gives /docs/_api/v1/traces.
function otlpBaseUrl(): string {
    const basePath = docsConfig.apiBasePath.replace(/\/+$/, '')
    return new URL(`${basePath}/`, window.location.origin).href
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
