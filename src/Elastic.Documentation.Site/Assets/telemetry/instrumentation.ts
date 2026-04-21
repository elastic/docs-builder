/**
 * OpenTelemetry configuration for frontend telemetry.
 * Sends traces and logs to the backend OTLP proxy endpoint.
 *
 * This module should be imported once at application startup.
 * All web components will automatically be instrumented once initialized.
 *
 * Inspired by: https://signoz.io/docs/frontend-monitoring/sending-logs-with-opentelemetry/
 */
import { config as docsConfig } from '../config'
import { logs } from '@opentelemetry/api-logs'
import { ZoneContextManager } from '@opentelemetry/context-zone'
import { W3CTraceContextPropagator } from '@opentelemetry/core'
import { OTLPLogExporter } from '@opentelemetry/exporter-logs-otlp-http'
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http'
import { registerInstrumentations } from '@opentelemetry/instrumentation'
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch'
import {
    defaultResource,
    resourceFromAttributes,
} from '@opentelemetry/resources'
import {
    LoggerProvider,
    BatchLogRecordProcessor,
    type LogRecordProcessor,
    type SdkLogRecord,
} from '@opentelemetry/sdk-logs'
import {
    WebTracerProvider,
    BatchSpanProcessor,
    SpanProcessor,
    Span,
} from '@opentelemetry/sdk-trace-web'
import {
    ATTR_SERVICE_NAME,
    ATTR_SERVICE_VERSION,
} from '@opentelemetry/semantic-conventions'

let isInitialized = false
let traceProvider: WebTracerProvider | null = null
let loggerProvider: LoggerProvider | null = null

export function initializeOtel(options: OtelConfigOptions = {}): boolean {
    if (isAlreadyInitialized()) return false

    markAsInitialized()

    const config = resolveConfiguration(options)
    logInitializationStart(config)

    try {
        const resource = createSharedResource(config)
        const commonHeaders = createCommonHeaders()

        initializeTracing(resource, config, commonHeaders)
        initializeLogging(resource, config, commonHeaders)

        setupAutoFlush(config.debug)
        logInitializationSuccess(config)

        return true
    } catch (error) {
        logInitializationError(error)
        isInitialized = false
        return false
    }
}

function isAlreadyInitialized(): boolean {
    if (isInitialized) {
        console.warn(
            'OpenTelemetry already initialized. Skipping re-initialization.'
        )
        return true
    }
    return false
}

function markAsInitialized(): void {
    isInitialized = true
}

function resolveConfiguration(options: OtelConfigOptions): ResolvedConfig {
    return {
        serviceName: options.serviceName ?? 'docs-frontend',
        serviceVersion: options.serviceVersion ?? '1.0.0',
        deploymentEnvironment: detectEnvironment(),
        baseUrl: options.baseUrl ?? window.location.origin,
        debug: options.debug ?? false,
    }
}

function logInitializationStart(config: ResolvedConfig): void {
    if (config.debug) {
        // eslint-disable-next-line no-console
        console.log('[OTEL] Initializing OpenTelemetry with config:', config)
    }
}

function createSharedResource(config: ResolvedConfig) {
    const resourceAttributes: Record<string, string> = {
        [ATTR_SERVICE_NAME]: config.serviceName,
        [ATTR_SERVICE_VERSION]: config.serviceVersion,
        ['deployment.environment']: config.deploymentEnvironment,
        ['service.language.name']: 'javascript',
    }
    // Merge with default resource to preserve telemetry.sdk.* attributes
    return defaultResource().merge(resourceFromAttributes(resourceAttributes))
}

function createCommonHeaders(): Record<string, string> {
    return {
        'X-Docs-Session': 'active',
    }
}

function initializeTracing(
    resource: ReturnType<typeof defaultResource>,
    config: ResolvedConfig,
    commonHeaders: Record<string, string>
): void {
    const traceExporter = new OTLPTraceExporter({
        url: `${docsConfig.apiBasePath}/v1/o/t`,
        headers: { ...commonHeaders },
    })

    const spanProcessor = new BatchSpanProcessor(traceExporter)
    const euidProcessor = new EuidSpanProcessor()

    traceProvider = new WebTracerProvider({
        resource,
        spanProcessors: [euidProcessor, spanProcessor],
    })

    traceProvider.register({
        contextManager: new ZoneContextManager(),
        propagator: new W3CTraceContextPropagator(),
    })

    registerFetchInstrumentation()
}

function registerFetchInstrumentation(): void {
    registerInstrumentations({
        instrumentations: [
            new FetchInstrumentation({
                propagateTraceHeaderCorsUrls: [
                    new RegExp(`${window.location.origin}/.*`),
                ],
                ignoreUrls: [
                    /_api\/v1\/o\/.*/,
                    /_api\/v1\/?$/,
                    /__parcel_code_frame$/,
                ],
            }),
        ],
    })
}

function initializeLogging(
    resource: ReturnType<typeof defaultResource>,
    config: ResolvedConfig,
    commonHeaders: Record<string, string>
): void {
    const logExporter = new OTLPLogExporter({
        url: `${docsConfig.apiBasePath}/v1/o/l`,
        headers: { ...commonHeaders },
    })

    const batchLogProcessor = new BatchLogRecordProcessor(logExporter)
    const euidLogProcessor = new EuidLogRecordProcessor()

    loggerProvider = new LoggerProvider({
        resource,
        processors: [euidLogProcessor, batchLogProcessor],
    })

    logs.setGlobalLoggerProvider(loggerProvider)
}

function setupAutoFlush(debug: boolean = false) {
    let isFlushing = false

    const performFlush = async () => {
        if (isFlushing || !isInitialized) {
            return
        }

        isFlushing = true

        if (debug) {
            // eslint-disable-next-line no-console
            console.log(
                '[OTEL] Auto-flushing telemetry (visibilitychange or pagehide)'
            )
        }

        try {
            await flushTelemetry()
        } catch (error) {
            if (debug) {
                console.warn('[OTEL] Error during auto-flush:', error)
            }
        } finally {
            isFlushing = false
        }
    }

    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'hidden') {
            performFlush()
        }
    })

    window.addEventListener('pagehide', performFlush)

    if (debug) {
        // eslint-disable-next-line no-console
        console.log('[OTEL] Auto-flush event listeners registered')
        // eslint-disable-next-line no-console
        console.log(
            '[OTEL] Using OTLP HTTP exporters with keepalive for guaranteed delivery'
        )
    }
}

async function flushTelemetry(timeoutMs: number = 1000): Promise<void> {
    if (!isInitialized) {
        return
    }

    const flushPromises: Promise<void>[] = []

    if (traceProvider) {
        flushPromises.push(
            traceProvider.forceFlush().catch((err) => {
                console.warn('[OTEL] Failed to flush traces:', err)
            })
        )
    }

    if (loggerProvider) {
        flushPromises.push(
            loggerProvider.forceFlush().catch((err) => {
                console.warn('[OTEL] Failed to flush logs:', err)
            })
        )
    }

    await Promise.race([
        Promise.all(flushPromises),
        new Promise<void>((resolve) => setTimeout(resolve, timeoutMs)),
    ])
}

function logInitializationSuccess(config: ResolvedConfig): void {
    if (config.debug) {
        // eslint-disable-next-line no-console
        console.log('[OTEL] OpenTelemetry initialized successfully', {
            serviceName: config.serviceName,
            serviceVersion: config.serviceVersion,
            deploymentEnvironment: config.deploymentEnvironment,
            traceEndpoint: `${docsConfig.apiBasePath}/v1/o/t`,
            logEndpoint: `${docsConfig.apiBasePath}/v1/o/l`,
            autoFlushOnUnload: true,
        })
    }
}

function logInitializationError(error: unknown): void {
    console.error('[OTEL] Failed to initialize OpenTelemetry:', error)
}

function getCookie(name: string): string | null {
    const value = `; ${document.cookie}`
    const parts = value.split(`; ${name}=`)
    if (parts.length === 2) return parts.pop()?.split(';').shift() || null
    return null
}

class EuidSpanProcessor implements SpanProcessor {
    onStart(span: Span): void {
        const euid = getCookie('euid')
        if (euid) {
            span.setAttribute('user.euid', euid)
        }
    }

    onEnd(): void {}

    shutdown(): Promise<void> {
        return Promise.resolve()
    }

    forceFlush(): Promise<void> {
        return Promise.resolve()
    }
}

class EuidLogRecordProcessor implements LogRecordProcessor {
    onEmit(logRecord: SdkLogRecord): void {
        const euid = getCookie('euid')
        if (euid) {
            logRecord.setAttribute('user.euid', euid)
        }
    }

    shutdown(): Promise<void> {
        return Promise.resolve()
    }

    forceFlush(): Promise<void> {
        return Promise.resolve()
    }
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

interface ResolvedConfig {
    serviceName: string
    serviceVersion: string
    deploymentEnvironment: string
    baseUrl: string
    debug: boolean
}
