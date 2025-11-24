/**
 * Logging utilities for frontend application.
 * Provides structured logging functions that send logs to the backend via OTLP.
 *
 * Based on: https://signoz.io/docs/frontend-monitoring/sending-logs-with-opentelemetry/
 */
import { logs, SeverityNumber, type AnyValueMap } from '@opentelemetry/api-logs'

const logger = logs.getLogger('docs-frontend-logger')

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
    logger.emit({
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
    logger.emit({
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
    logger.emit({
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
    logger.emit({
        body,
        severityNumber: SeverityNumber.DEBUG,
        severityText: 'DEBUG',
        attributes: attrs,
    })
}
