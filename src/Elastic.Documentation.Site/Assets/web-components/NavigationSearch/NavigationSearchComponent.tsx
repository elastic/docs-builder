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
    /** `sidebar`: the host owns spacing and the divider, so render the bare input. */
    variant?: string
}

const parseTypeFilter = (value: string | undefined): TypeFilter =>
    value === 'docs' || value === 'api' ? value : 'all'

export const NavigationSearchWrapper = ({
    placeholder,
    type,
    variant,
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
                        variant={variant}
                    />
                </QueryClientProvider>
            </EuiProvider>
        </StrictMode>
    )
}

const NavigationSearchInner = ({
    placeholder,
    type,
    variant,
}: NavigationSearchProps) => {
    const { euiTheme } = useEuiTheme()
    const typeFilter = parseTypeFilter(type)
    const resolvedPlaceholder =
        placeholder ?? (typeFilter === 'api' ? 'Find in all APIs' : undefined)

    if (config.airGapped) {
        return null
    }

    const search = (
        <NavigationSearch
            size={variant === 'sidebar' ? 's' : undefined}
            placeholder={resolvedPlaceholder}
            typeFilter={typeFilter}
        />
    )

    if (variant === 'sidebar') {
        return search
    }

    return (
        <div
            className="sticky top-0"
            css={css`
                padding-top: ${euiTheme.size.base};
                padding-right: ${euiTheme.size.base};
            `}
        >
            {search}
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
            variant: 'string',
        },
    })
)
