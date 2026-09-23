import { config } from '../../config'
import '../../eui-icons-cache'
import { sharedQueryClient } from '../shared/queryClient'
import { NavigationSearch } from './NavigationSearch'
import { type TypeFilter } from './useNavigationSearchQuery'
import { EuiHorizontalRule, EuiProvider, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'
import r2wc from '@r2wc/react-to-web-component'
import { QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'

interface NavigationSearchProps {
    placeholder?: string
    type?: string
}

const parseTypeFilter = (value: string | undefined): TypeFilter =>
    value === 'docs' || value === 'api' ? value : 'all'

export const NavigationSearchWrapper = ({
    placeholder,
    type,
}: NavigationSearchProps) => {
    return (
        <StrictMode>
            <EuiProvider
                colorMode="light"
                globalStyles={false}
                utilityClasses={false}
            >
                <QueryClientProvider client={sharedQueryClient}>
                    <NavigationSearchInner
                        placeholder={placeholder}
                        type={type}
                    />
                </QueryClientProvider>
            </EuiProvider>
        </StrictMode>
    )
}

const NavigationSearchInner = ({
    placeholder,
    type,
}: NavigationSearchProps) => {
    const { euiTheme } = useEuiTheme()
    const typeFilter = parseTypeFilter(type)

    if (config.airGapped) {
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
            <NavigationSearch
                placeholder={
                    placeholder ??
                    (typeFilter === 'api' ? 'Jump to API' : undefined)
                }
                typeFilter={typeFilter}
            />
            <EuiHorizontalRule
                margin="none"
                css={css`
                    margin-top: ${euiTheme.size.base};
                `}
            />
        </div>
    )
}

customElements.define(
    'navigation-search',
    r2wc(NavigationSearchWrapper, {
        props: {
            placeholder: 'string',
            type: 'string',
        },
    })
)
