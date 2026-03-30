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
        enabled: config.buildType !== 'codex',
    })

    if (!isApiAvailable && config.buildType !== 'codex') {
        return null
    }

    return (
        <div
            className="sticky top-0"
            css={css`
                padding-top: 24px;
                padding-right: 24px;
                padding-bottom: 24px;
                border-bottom: 1px solid var(--color-grey-20, #e5e9f0);
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
