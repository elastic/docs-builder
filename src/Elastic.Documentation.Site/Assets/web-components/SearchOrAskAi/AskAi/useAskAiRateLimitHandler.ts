import { ApiError } from '../errorHandling'
import { useRateLimitHandler } from '../useRateLimitHandler'

export function useAskAiRateLimitHandler(error: ApiError | Error | null) {
    useRateLimitHandler('askAi', error)
}
