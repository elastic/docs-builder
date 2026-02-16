/**
 * Logging utilities for frontend application.
 * Provides structured logging functions that send logs to the backend via OTLP.
 *
 * Based on: https://signoz.io/docs/frontend-monitoring/sending-logs-with-opentelemetry/
 */
import { config } from '../config'
import { logs, SeverityNumber, type AnyValueMap } from '@opentelemetry/api-logs'

// Lazy-initialize the logger to avoid errors when this module is imported
// before the OpenTelemetry LoggerProvider is set up
function getLogger() {
    return logs.getLogger(`${config.serviceName}-logger`)
}

/**
 * Log an informational message.
 *
 * @param body The log message
 * @param attrs Additional attributes to attach to the log
 *
 * @example
 * ```ts
 * logInfo('User clicked search button', {
 *   'user.action': 'search',
 *   'search.query': query
 * })
 * ```
 */
export function logInfo(body: string, attrs: AnyValueMap = {}) {
    getLogger().emit({
        body,
        severityNumber: SeverityNumber.INFO,
        severityText: 'INFO',
        attributes: attrs,
    })
}

/**
 * Log a warning message.
 *
 * @param body The log message
 * @param attrs Additional attributes to attach to the log
 *
 * @example
 * ```ts
 * logWarn('Search returned no results', {
 *   'search.query': query,
 *   'search.duration_ms': duration
 * })
 * ```
 */
export function logWarn(body: string, attrs: AnyValueMap = {}) {
    getLogger().emit({
        body,
        severityNumber: SeverityNumber.WARN,
        severityText: 'WARN',
        attributes: attrs,
    })
}

/**
 * Log an error message.
 *
 * @param body The log message
 * @param attrs Additional attributes to attach to the log
 *
 * @example
 * ```ts
 * logError('Failed to fetch search results', {
 *   'error.message': error.message,
 *   'error.stack': error.stack,
 *   'search.query': query
 * })
 * ```
 */
export function logError(body: string, attrs: AnyValueMap = {}) {
    getLogger().emit({
        body,
        severityNumber: SeverityNumber.ERROR,
        severityText: 'ERROR',
        attributes: attrs,
    })
}

/**
 * Log a debug message (only useful in development).
 *
 * @param body The log message
 * @param attrs Additional attributes to attach to the log
 *
 * @example
 * ```ts
 * logDebug('Component rendered', {
 *   'component.name': 'SearchResults',
 *   'render.time_ms': renderTime
 * })
 * ```
 */
export function logDebug(body: string, attrs: AnyValueMap = {}) {
    getLogger().emit({
        body,
        severityNumber: SeverityNumber.DEBUG,
        severityText: 'DEBUG',
        attributes: attrs,
    })
}
