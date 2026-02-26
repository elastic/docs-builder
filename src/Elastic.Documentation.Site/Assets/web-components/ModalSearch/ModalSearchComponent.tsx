import { config } from '../../config'
import '../../eui-icons-cache'
import { ModalSearch } from './ModalSearch'
import { EuiProvider } from '@elastic/eui'
import r2wc from '@r2wc/react-to-web-component'
import {
    QueryClient,
    QueryClientProvider,
    useQuery,
} from '@tanstack/react-query'
import { StrictMode } from 'react'

const queryClient = new QueryClient()

const ModalSearchInner = ({ placeholder }: { placeholder?: string }) => {
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

    return <ModalSearch placeholder={placeholder} />
}

const ModalSearchWrapper = ({ placeholder }: { placeholder?: string }) => {
    return (
        <StrictMode>
            <EuiProvider
                colorMode="light"
                globalStyles={false}
                utilityClasses={false}
            >
                <QueryClientProvider client={queryClient}>
                    <ModalSearchInner placeholder={placeholder} />
                </QueryClientProvider>
            </EuiProvider>
        </StrictMode>
    )
}

customElements.define(
    'modal-search',
    r2wc(ModalSearchWrapper, {
        props: {
            placeholder: 'string',
        },
    })
)
