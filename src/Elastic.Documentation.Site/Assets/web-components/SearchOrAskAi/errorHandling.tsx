import { ReactNode } from 'react'
import * as z from 'zod'

export type ErrorCode = '429' | '503' | '4xx' | '5xx' | 'unknown'

export const ApiErrorSchema = z.object({
    message: z.string(),
    statusCode: z.number(),
    retryAfter: z.number().optional(),
    rateLimitScope: z.enum(['per-user', 'global']).optional(),
})

export type ApiError = z.infer<typeof ApiErrorSchema> & Error

export function getErrorCode(statusCode: number): ErrorCode {
    if (statusCode === 429) return '429'
    if (statusCode === 503) return '503'
    if (statusCode >= 400 && statusCode < 500) return '4xx'
    if (statusCode >= 500) return '5xx'
    return 'unknown'
}

export function getErrorMessage(error: ApiError | Error | null): ReactNode {
    if (isApiError(error)) {
        switch (getErrorCode(error.statusCode)) {
            case '429':
                return (
                    <p>
                        You have reached the temporary request limit. Wait{' '}
                        {error.retryAfter} second
                        {error.retryAfter !== 1 ? 's' : ''}, then try again.
                    </p>
                )
            case '503':
                return (
                    <p>
                        An unexpected error occurred. Wait a few seconds, then
                        try again.
                    </p>
                )
            case '4xx':
                return (
                    <>
                        <p>
                            We are unable to process your request.
                            <br />
                            Try rephrasing your question. For example, &quot;How
                            do I configure an index in Elasticsearch?&quot;
                        </p>
                        <p>
                            If you think this is a bug, open a{' '}
                            <a
                                href="https://github.com/elastic/docs-builder/issues/new?template=bug-report.yaml"
                                target="_blank"
                                rel="noopener noreferrer"
                            >
                                GitHub issue.
                            </a>
                        </p>
                    </>
                )
            case '5xx':
                return (
                    <>
                        <p>We are unable to process your request.</p>
                        <p>
                            If you think this is a bug, open a{' '}
                            <a
                                href="https://github.com/elastic/docs-builder/issues/new?template=bug-report.yaml"
                                target="_blank"
                                rel="noopener noreferrer"
                            >
                                GitHub issue.
                            </a>
                        </p>
                    </>
                )
        }
    }
    return <p>An unexpected error occurred. Please try again.</p>
}

export async function createApiErrorFromResponse(
    response: Response
): Promise<ApiError | null> {
    const statusCode = response.status

    try {
        const message = await response.text().catch((error) => error.message)

        const errorSchema = ApiErrorSchema.parse({
            message: message ?? '',
            statusCode: statusCode,
        })
        if (statusCode === 429 || statusCode === 503) { // Check raw statusCode for retryable errors
            const retryAfterHeader = response.headers.get('Retry-After')
            const rateLimitScopeHeader =
                response.headers.get('X-Rate-Limit-Scope')

            if (retryAfterHeader != null) {
                errorSchema.retryAfter = parseInt(retryAfterHeader, 10)
            }
            if (rateLimitScopeHeader != null) {
                const rateLimitScope =
                    rateLimitScopeHeader === 'per-user' ||
                    rateLimitScopeHeader === 'global'
                        ? rateLimitScopeHeader
                        : undefined
                errorSchema.rateLimitScope = rateLimitScope as
                    | 'per-user'
                    | 'global'
                    | undefined
            }
        }

        const error = new Error(errorSchema.message) as ApiError
        error.name = 'ApiError'
        error.statusCode = errorSchema.statusCode
        error.retryAfter = errorSchema.retryAfter
        error.rateLimitScope = errorSchema.rateLimitScope
        return error
    } catch (err) {
        const errorSchema = ApiErrorSchema.parse({
            message: `Request failed with status ${statusCode}. Error: ${(err as Error)?.message ?? 'Unknown error'}`,
            statusCode: statusCode,
            retryAfter: undefined,
            rateLimitScope: undefined,
        })
        const error = new Error(errorSchema.message) as ApiError
        error.name = 'ApiError'
        error.statusCode = errorSchema.statusCode
        return error
    }
}

export function shouldRetry(
    failureCount: number,
    error: ApiError | null
): boolean {
    // Don't retry if we've exhausted retries
    if (failureCount >= 3) return false
    // Don't retry for 429 (rate limit) or 503 (service unavailable)
    if (error && isRetryableError(error)) {
        return false
    }
    // Retry for other errors (up to 3 times)
    return true
}

export function isApiError(error: ApiError | Error | null): error is ApiError {
    return (
        error instanceof Error &&
        'statusCode' in error &&
        error.name === 'ApiError'
    )
}

/**
 * Checks if an error is a rate limit error (429).
 */
export function isRateLimitError(error: ApiError | Error | null): boolean {
    return isApiError(error) && error.statusCode === 429
}

/**
 * Checks if an error is retryable (429 or 503).
 */
export function isRetryableError(error: ApiError | Error | null): boolean {
    return isApiError(error) && (error.statusCode === 429 || error.statusCode === 503)
}
