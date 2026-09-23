import { ModalSearch } from './ModalSearch'
import { modalSearchStore } from './modalSearch.store'
import { EuiProvider } from '@elastic/eui'
import { act, render, screen } from '@testing-library/react'

jest.mock('../AskAi/InfoBanner', () => ({
    InfoBanner: () => null,
}))

jest.mock('../AskAi/KeyboardShortcutsFooter', () => ({
    KeyboardShortcutsFooter: () => null,
}))

jest.mock('./useModalSearchQuery', () => ({
    useModalSearchQuery: () => ({
        isLoading: false,
        isFetching: false,
        data: { results: [] },
        error: null,
    }),
}))

jest.mock('./useModalSearchTelemetry', () => ({
    useModalSearchTelemetry: () => ({
        trackOpened: jest.fn(),
        trackClosed: jest.fn(),
    }),
}))

const renderModalSearch = () =>
    render(
        <EuiProvider
            colorMode="light"
            globalStyles={false}
            utilityClasses={false}
        >
            <ModalSearch />
        </EuiProvider>
    )

describe('ModalSearch', () => {
    beforeEach(() => {
        act(() => {
            modalSearchStore.getState().actions.closeModal()
        })
    })

    it('stays mounted during an HTMX request and closes after the swap', () => {
        renderModalSearch()

        act(() => {
            modalSearchStore.getState().actions.openModal()
        })

        act(() => {
            document.dispatchEvent(new CustomEvent('htmx:beforeSend'))
        })
        expect(
            screen.getByRole('button', { name: 'Close search modal' })
        ).toBeInTheDocument()

        act(() => {
            document.dispatchEvent(new CustomEvent('htmx:afterSwap'))
        })
        expect(modalSearchStore.getState().isOpen).toBe(false)
        expect(
            screen.queryByRole('button', { name: 'Close search modal' })
        ).not.toBeInTheDocument()
    })

    it('does not offer Ask AI in an isolated build', () => {
        renderModalSearch()

        act(() => {
            modalSearchStore.getState().actions.openModal()
            modalSearchStore.getState().actions.setSearchTerm('logging')
        })

        expect(screen.queryByText('Ask AI Assistant')).not.toBeInTheDocument()
        expect(screen.queryByText('Tell me more about')).not.toBeInTheDocument()
    })
})
