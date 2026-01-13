import { ApiError } from '../shared/errorHandling'
import { useRateLimitHandler } from '../shared/useRateLimitHandler'

export function useAskAiRateLimitHandler(error: ApiError | Error | null) {
    useRateLimitHandler('askAi', error)
}
