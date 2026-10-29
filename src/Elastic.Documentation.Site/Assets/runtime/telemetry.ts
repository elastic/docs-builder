import { config } from '../config'

// Injected at build time from MinVer
const DOCS_BUILDER_VERSION =
    process.env.DOCS_BUILDER_VERSION?.trim() ?? '0.0.0-dev'

// Initialize OpenTelemetry before the web components when telemetry is enabled, so
// their instrumented work runs after init. The OTel SDK is dynamically imported, so
// pages built with telemetry disabled never fetch it.
export async function initTelemetry() {
    if (!config.telemetryEnabled) return

    const { initializeOtel } = await import('../telemetry/instrumentation')
    initializeOtel({
        serviceName: config.serviceName,
        serviceVersion: DOCS_BUILDER_VERSION,
        baseUrl: config.rootPath,
        debug: false,
    })
}
