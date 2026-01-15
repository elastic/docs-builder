import { useQuery } from '@tanstack/react-query'

// Demo mode: Read fail parameter once at module load
const getFailMode = () => {
    if (typeof window === 'undefined') return null
    const params = new URLSearchParams(window.location.search)
    return params.get('fail')
}
const FAIL_MODE = getFailMode()

export type AvailabilityStatus = 'checking' | 'available' | 'unavailable'

interface AvailabilityResult {
    status: AvailabilityStatus
    isAvailable: boolean
    isChecking: boolean
}

export const useSearchAvailability = (): AvailabilityResult => {
    const { data, isLoading, isError } = useQuery({
        queryKey: ['search-availability', FAIL_MODE],
        queryFn: async () => {
            // Demo mode: fail=unavailable simulates service unavailability
            if (FAIL_MODE === 'unavailable') {
                return { available: false }
            }

            try {
                const response = await fetch('/docs/_api/v1/')

                if (response.status === 403 || response.status >= 500) {
                    return { available: false }
                }

                if (response.ok) {
                    return { available: true }
                }

                // For other non-OK responses, treat as unavailable
                return { available: false }
            } catch {
                return { available: false }
            }
        },
        staleTime: 1000 * 60 * 5, // 5 minutes
        retry: false,
        refetchOnWindowFocus: false,
    })

    if (isLoading) {
        return {
            status: 'checking',
            isAvailable: false,
            isChecking: true,
        }
    }

    if (isError || !data?.available) {
        return {
            status: 'unavailable',
            isAvailable: false,
            isChecking: false,
        }
    }

    return {
        status: 'available',
        isAvailable: true,
        isChecking: false,
    }
}
