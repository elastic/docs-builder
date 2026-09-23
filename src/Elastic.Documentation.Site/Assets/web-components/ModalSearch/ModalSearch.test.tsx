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

    it('keeps the result link mounted until the HTMX request finishes', () => {
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

        expect(modalSearchStore.getState().isOpen).toBe(true)
        expect(
            screen.getByRole('button', {
                name: 'Close search modal',
                hidden: true,
            })
        ).toBeInTheDocument()
        expect(
            screen.queryByRole('button', { name: 'Close search modal' })
        ).not.toBeInTheDocument()

        act(() => {
            document.dispatchEvent(
                new CustomEvent('htmx:afterRequest', {
                    detail: { elt: result, successful: true },
                })
            )
        })

        expect(modalSearchStore.getState().isOpen).toBe(false)
        expect(
            screen.queryByRole('button', {
                name: 'Close search modal',
                hidden: true,
            })
        ).not.toBeInTheDocument()
    })

    it('shows the modal again when a result navigation fails', () => {
        renderModalSearch()

        act(() => {
            modalSearchStore.getState().actions.openModal()
        })

        const result = document.createElement('a')
        result.setAttribute('data-search-result-index', '0')

        act(() => {
            document.dispatchEvent(
                new CustomEvent('htmx:beforeSend', {
                    detail: { elt: result },
                })
            )
            document.dispatchEvent(
                new CustomEvent('htmx:afterRequest', {
                    detail: { elt: result, successful: false },
                })
            )
        })

        expect(modalSearchStore.getState().isOpen).toBe(true)
        expect(
            screen.getByRole('button', { name: 'Close search modal' })
        ).toBeInTheDocument()
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
