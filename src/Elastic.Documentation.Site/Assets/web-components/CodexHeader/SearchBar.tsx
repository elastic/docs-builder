import '../../eui-icons-cache'
import { AskAiHeaderButton } from '../AskAi/AskAiHeaderButton'
import { ModalSearch } from '../ModalSearch/ModalSearch'
import { sharedQueryClient } from '../shared/queryClient'
import { EuiProvider } from '@elastic/eui'
import r2wc from '@r2wc/react-to-web-component'
import { QueryClientProvider } from '@tanstack/react-query'

export const SearchBar = () => (
    <QueryClientProvider client={sharedQueryClient}>
        <EuiProvider
            colorMode="light"
            globalStyles={false}
            utilityClasses={false}
        >
            <div className="flex items-center gap-1 min-w-0">
                <div className="min-w-0 md:min-w-60 shrink">
                    <ModalSearch size="s" placeholder="Search" />
                </div>
                <AskAiHeaderButton />
            </div>
        </EuiProvider>
    </QueryClientProvider>
)

customElements.define('codex-search-bar', r2wc(SearchBar))
