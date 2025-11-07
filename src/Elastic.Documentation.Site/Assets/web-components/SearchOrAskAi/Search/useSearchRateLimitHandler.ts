import { ApiError } from '../errorHandling'
import { useRateLimitHandler } from '../useRateLimitHandler'

export function useSearchRateLimitHandler(error: ApiError | Error | null) {
    useRateLimitHandler('search', error)
}
