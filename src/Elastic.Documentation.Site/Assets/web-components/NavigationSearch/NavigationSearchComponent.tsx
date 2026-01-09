import '../../eui-icons-cache'
import { NavigationSearch } from './NavigationSearch'
import { EuiProvider } from '@elastic/eui'
import r2wc from '@r2wc/react-to-web-component'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'

const queryClient = new QueryClient()

const NavigationSearchWrapper = () => {
    return (
        <StrictMode>
            <EuiProvider
                colorMode="light"
                globalStyles={false}
                utilityClasses={false}
            >
                <QueryClientProvider client={queryClient}>
                    <NavigationSearch />
                </QueryClientProvider>
            </EuiProvider>
        </StrictMode>
    )
}

customElements.define('navigation-search', r2wc(NavigationSearchWrapper))
