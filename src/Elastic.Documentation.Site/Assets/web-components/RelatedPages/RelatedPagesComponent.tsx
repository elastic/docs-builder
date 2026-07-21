import { sharedQueryClient } from '../shared/queryClient'
import { RelatedPages } from './RelatedPages'
import { EuiProvider } from '@elastic/eui'
import r2wc from '@r2wc/react-to-web-component'
import { QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'

const RelatedPagesWrapper = () => (
    <StrictMode>
        <EuiProvider
            colorMode="light"
            globalStyles={false}
            utilityClasses={false}
        >
            <QueryClientProvider client={sharedQueryClient}>
                <RelatedPages />
            </QueryClientProvider>
        </EuiProvider>
    </StrictMode>
)

customElements.define('related-pages', r2wc(RelatedPagesWrapper))
