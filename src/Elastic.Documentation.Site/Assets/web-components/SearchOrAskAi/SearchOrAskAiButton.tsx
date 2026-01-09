import '../../eui-icons-cache'
import { NavigationSearch } from './NavigationSearch'
import { useQuery } from '@tanstack/react-query'

export const SearchOrAskAiButton = () => {
    const { data: isApiAvailable } = useQuery({
        queryKey: ['api-health'],
        queryFn: async () => {
            const response = await fetch('/docs/_api/v1/', { method: 'POST' })
            return response.ok
        },
        staleTime: 60 * 60 * 1000, // 60 minutes
        retry: false,
    })

    if (!isApiAvailable) {
        return null
    }

    return <NavigationSearch />
}
