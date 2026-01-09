import '../../eui-icons-cache'
import { NavigationSearch } from './NavigationSearch'
import { EuiProvider } from '@elastic/eui'
import r2wc from '@r2wc/react-to-web-component'
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query'
import { StrictMode } from 'react'

const queryClient = new QueryClient()

const NavigationSearchInner = () => {
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

const NavigationSearchWrapper = () => {
    return (
        <StrictMode>
            <EuiProvider
                colorMode="light"
                globalStyles={false}
                utilityClasses={false}
            >
                <QueryClientProvider client={queryClient}>
                    <NavigationSearchInner />
                </QueryClientProvider>
            </EuiProvider>
        </StrictMode>
    )
}

customElements.define('navigation-search', r2wc(NavigationSearchWrapper))
