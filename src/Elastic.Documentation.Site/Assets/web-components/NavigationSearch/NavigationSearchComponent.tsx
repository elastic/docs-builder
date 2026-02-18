import '../../eui-icons-cache'
import { sharedQueryClient } from '../shared/queryClient'
import { NavigationSearch } from './NavigationSearch'
import { EuiHorizontalRule, EuiProvider, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'
import r2wc from '@r2wc/react-to-web-component'
import { QueryClientProvider, useQuery } from '@tanstack/react-query'
import { StrictMode } from 'react'

const NavigationSearchInner = () => {
    const { euiTheme } = useEuiTheme()
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

    return (
        <div
            className="sticky top-0"
            css={css`
                padding-top: ${euiTheme.size.base};
                padding-right: ${euiTheme.size.base};
            `}
        >
            <NavigationSearch />
            <EuiHorizontalRule
                margin="none"
                css={css`
                    margin-top: ${euiTheme.size.base};
                `}
            />
        </div>
    )
}

const NavigationSearchWrapper = () => {
    return (
        <StrictMode>
            <EuiProvider
                colorMode="light"
                globalStyles={false}
                utilityClasses={false}
            >
                <QueryClientProvider client={sharedQueryClient}>
                    <NavigationSearchInner />
                </QueryClientProvider>
            </EuiProvider>
        </StrictMode>
    )
}

customElements.define('navigation-search', r2wc(NavigationSearchWrapper))
