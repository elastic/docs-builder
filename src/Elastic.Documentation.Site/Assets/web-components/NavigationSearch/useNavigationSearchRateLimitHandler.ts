import { ApiError } from '../shared/errorHandling'
import { useRateLimitHandler } from '../shared/useRateLimitHandler'

export function useNavigationSearchRateLimitHandler(
    error: ApiError | Error | null
) {
    useRateLimitHandler('search', error)
}
