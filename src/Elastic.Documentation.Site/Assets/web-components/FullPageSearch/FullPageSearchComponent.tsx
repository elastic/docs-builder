import { config } from '../../config'
import '../../eui-icons-cache'
import { FullPageSearch } from './FullPageSearch'
import { EuiProvider } from '@elastic/eui'
import r2wc from '@r2wc/react-to-web-component'
import {
    QueryClient,
    QueryClientProvider,
    useQuery,
} from '@tanstack/react-query'
import { StrictMode } from 'react'

const queryClient = new QueryClient()

const FullPageSearchInner = () => {
    const { data: isApiAvailable } = useQuery({
        queryKey: ['api-health'],
        queryFn: async () => {
            const response = await fetch(`${config.apiBasePath}/v1/`, {
                method: 'POST',
            })
            return response.ok
        },
        staleTime: 60 * 60 * 1000, // 60 minutes
        retry: false,
    })

    if (!isApiAvailable) {
        return null
    }

    return <FullPageSearch />
}

const FullPageSearchWrapper = () => {
    return (
        <StrictMode>
            <EuiProvider
                colorMode="light"
                globalStyles={false}
                utilityClasses={false}
            >
                <QueryClientProvider client={queryClient}>
                    <FullPageSearchInner />
                </QueryClientProvider>
            </EuiProvider>
        </StrictMode>
    )
}

customElements.define('full-page-search', r2wc(FullPageSearchWrapper))
