import { apiPageSearchParams } from '../api-tree'
import { useMemo } from 'react'

/** The API page parameters for the current path. htmx swaps pages without remounting, so the path is read on every render. */
export const useApiPageSearchParams = (): Record<string, string> => {
    const { pathname } = location
    return useMemo(() => apiPageSearchParams(pathname), [pathname])
}
