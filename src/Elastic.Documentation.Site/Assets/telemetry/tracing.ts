/**
 * React utilities for OpenTelemetry tracing in components.
 */
import { trace, context, SpanStatusCode, Span } from '@opentelemetry/api'

export async function traceSpan<T>(
    spanName: string,
    fn: (span: Span) => Promise<T>,
    attributes?: Record<string, string | number | boolean>
): Promise<T> {
    const tracer = trace.getTracer('docs-frontend')
    const span = tracer.startSpan(spanName, undefined, context.active())

    if (attributes) {
        span.setAttributes(attributes)
    }

    try {
        const result = await fn(span)
        span.setStatus({ code: SpanStatusCode.OK })
        return result
    } catch (error) {
        // Check if this is an AbortError (user cancelled/typed more)
        if (error instanceof Error && error.name === 'AbortError') {
            // Cancellation is NOT an error - it's expected behavior
            span.setAttribute('cancelled', true)
            span.setStatus({ code: SpanStatusCode.OK })
        } else {
            // Real error - mark as ERROR
            span.setStatus({
                code: SpanStatusCode.ERROR,
                message: error instanceof Error ? error.message : String(error),
            })
            span.recordException(error as Error)
        }
        throw error
    } finally {
        span.end()
    }
}
