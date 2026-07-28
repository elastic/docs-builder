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

    it('closes before an HTMX navigation swaps the page', () => {
        renderModalSearch()

        act(() => {
            modalSearchStore.getState().actions.openModal()
        })
        expect(
            screen.getByRole('button', { name: 'Close search modal' })
        ).toBeInTheDocument()

        const result = document.createElement('a')
        result.setAttribute('data-search-result-index', '0')

        act(() => {
            document.dispatchEvent(
                new CustomEvent('htmx:beforeSend', {
                    detail: { elt: result },
                })
            )
        })

        expect(modalSearchStore.getState().isOpen).toBe(false)
        expect(
            screen.queryByRole('button', { name: 'Close search modal' })
        ).not.toBeInTheDocument()
    })
})
