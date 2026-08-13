import { config } from '../../config'
import '../../eui-icons-cache'
import { sharedQueryClient } from '../shared/queryClient'
import { NavigationSearch } from './NavigationSearch'
import { EuiProvider } from '@elastic/eui'
import { css } from '@emotion/react'
import r2wc from '@r2wc/react-to-web-component'
import { QueryClientProvider, useQuery } from '@tanstack/react-query'
import { StrictMode } from 'react'

interface NavigationSearchProps {
    placeholder?: string
}

const NavigationSearchInner = ({ placeholder }: NavigationSearchProps) => {
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
        enabled: config.buildType !== 'codex' && !config.airGapped,
    })

    if (config.airGapped || (!isApiAvailable && config.buildType !== 'codex')) {
        return null
    }

    return (
        <div
            className="pages-nav-v2__search-inner sticky top-0 z-10 shrink-0 bg-white"
            css={css`
                padding: 16px 19px 0 0;
            `}
        >
            <NavigationSearch placeholder={placeholder} />
        </div>
    )
}

const NavigationSearchWrapper = ({ placeholder }: NavigationSearchProps) => {
    return (
        <StrictMode>
            <EuiProvider
                colorMode="light"
                globalStyles={false}
                utilityClasses={false}
            >
                <QueryClientProvider client={sharedQueryClient}>
                    <NavigationSearchInner placeholder={placeholder} />
                </QueryClientProvider>
            </EuiProvider>
        </StrictMode>
    )
}

customElements.define(
    'navigation-search',
    r2wc(NavigationSearchWrapper, {
        props: {
            placeholder: 'string',
        },
    })
)
