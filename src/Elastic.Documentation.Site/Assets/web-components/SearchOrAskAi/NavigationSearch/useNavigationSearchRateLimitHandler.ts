import { ApiError } from '../errorHandling'
import { useRateLimitHandler } from '../useRateLimitHandler'

export function useNavigationSearchRateLimitHandler(
    error: ApiError | Error | null
) {
    useRateLimitHandler('search', error)
}
