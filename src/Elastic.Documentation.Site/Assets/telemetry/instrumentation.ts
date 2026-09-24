/**
 * Elastic EDOT browser SDK initialisation.
 * Sends traces and logs to the backend OTLP proxy endpoint.
 *
 * This module should be imported once at application startup.
 * All web components will automatically be instrumented once initialised.
 */
import { config as docsConfig } from '../config'
import { startBrowserSdk } from '@elastic/opentelemetry-browser'

let sdk: ReturnType<typeof startBrowserSdk> | null = null

export function initializeOtel(options: OtelConfigOptions = {}): boolean {
    if (isSyntheticMonitor()) return false
    if (sdk !== null) {
        console.warn(
            'OpenTelemetry already initialized. Skipping re-initialization.'
        )
        return false
    }

    try {
        sdk = startBrowserSdk({
            serviceName: options.serviceName ?? docsConfig.serviceName,
            serviceVersion: options.serviceVersion ?? '1.0.0',
            // apiBasePath is a relative path (/docs/_api or /api); EDOT appends /v1/traces and /v1/logs
            otlpEndpoint: docsConfig.apiBasePath,
            exportHeaders: { 'X-Docs-Session': 'active' },
            resourceAttributes: {
                'deployment.environment': detectEnvironment(),
                'service.language.name': 'javascript',
            },
            instrumentations: {
                '@opentelemetry/instrumentation-fetch': {
                    propagateTraceHeaderCorsUrls: [
                        new RegExp(`${window.location.origin}/.*`),
                    ],
                    // Avoid recursive telemetry: ignore the OTLP proxy paths and
                    // the root API availability check used by the search feature.
                    ignoreUrls: [
                        /\/v1\/(traces|logs)$/,
                        /\/_api\/v1\/?$/,
                        /__parcel_code_frame$/,
                    ],
                },
            },
        })

        setupAutoFlush()
        return true
    } catch (error) {
        console.error('[OTEL] Failed to initialize OpenTelemetry:', error)
        sdk = null
        return false
    }
}

// The backend excludes synthetic traffic via the X-Docs-Synthetic-Monitor header (see
// synthetics.config.ts / TelemetryConstants.SyntheticMonitorHeaderName), but page JS can't
// read its own outgoing request headers. @elastic/synthetics appends this to the browser's
// UA instead (unless a custom userAgent is configured), so we sniff that for the same effect.
function isSyntheticMonitor(): boolean {
    return navigator.userAgent.includes('Elastic/Synthetics')
}

function setupAutoFlush(): void {
    let isFlushing = false

    const performFlush = async () => {
        if (isFlushing || sdk === null) return
        isFlushing = true
        try {
            await sdk.forceFlush()
        } catch {
            // best-effort; flush errors should not surface to the user
        } finally {
            isFlushing = false
        }
    }

    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'hidden') performFlush()
    })
    window.addEventListener('pagehide', performFlush)
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
    debug?: boolean
}
