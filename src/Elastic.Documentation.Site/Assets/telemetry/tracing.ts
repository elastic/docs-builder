/**
 * React utilities for OpenTelemetry tracing in components.
 */
import { config } from '../config'
import { trace, SpanStatusCode, Span } from '@opentelemetry/api'

/**
 * Creates a span and executes a function within its context.
 *
 * This ensures that:
 * - The span is automatically set as the active context
 * - Any automatic instrumentation (e.g., fetch, XHR) creates child spans
 * - Nested calls to traceSpan() properly create child spans
 * - The span is properly ended even if errors occur
 *
 * @param spanName - Name of the span to create
 * @param fn - Async function to execute within the span context
 * @param attributes - Optional attributes to set on the span
 * @returns Promise resolving to the function's return value
 */
export async function traceSpan<T>(
    spanName: string,
    fn: (span: Span) => Promise<T>,
    attributes?: Record<string, string | number | boolean>
): Promise<T> {
    const tracer = trace.getTracer(config.serviceName)

    // startActiveSpan automatically:
    // 1. Creates the span
    // 2. Sets it as the active context
    // 3. Executes the callback within that context
    // 4. Ends the span after execution
    return tracer.startActiveSpan(spanName, async (span) => {
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
                    message:
                        error instanceof Error ? error.message : String(error),
                })
                span.recordException(error as Error)
            }
            throw error
        } finally {
            span.end()
        }
    })
}
