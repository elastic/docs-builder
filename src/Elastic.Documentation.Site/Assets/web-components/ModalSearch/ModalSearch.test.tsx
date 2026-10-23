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

    it('stays open until the HTMX swap is done, then closes', () => {
        renderModalSearch()

        act(() => {
            modalSearchStore.getState().actions.openModal()
        })
        const closeButton = () =>
            screen.queryByRole('button', { name: 'Close search modal' })
        expect(closeButton()).toBeInTheDocument()

        const result = document.createElement('a')
        result.setAttribute('data-search-result-index', '0')

        act(() => {
            document.dispatchEvent(
                new CustomEvent('htmx:beforeSend', { detail: { elt: result } })
            )
        })
        expect(modalSearchStore.getState().isOpen).toBe(true)
        expect(closeButton()).toBeInTheDocument()

        act(() => {
            document.dispatchEvent(new CustomEvent('htmx:afterSwap'))
        })
        expect(modalSearchStore.getState().isOpen).toBe(false)
        expect(closeButton()).not.toBeInTheDocument()
    })

    it('cancels a pending result navigation when the modal is dismissed', () => {
        renderModalSearch()

        act(() => {
            modalSearchStore.getState().actions.openModal()
        })

        const result = document.createElement('a')
        result.setAttribute('data-search-result-index', '0')
        const aborted = jest.fn()
        const onAbort = (event: Event) => {
            if ((event as CustomEvent).detail?.elt === result) aborted()
        }
        document.body.addEventListener('htmx:abort', onAbort)

        act(() => {
            document.dispatchEvent(
                new CustomEvent('htmx:beforeSend', { detail: { elt: result } })
            )
        })
        act(() => {
            modalSearchStore.getState().actions.closeModal()
        })

        document.body.removeEventListener('htmx:abort', onAbort)
        expect(aborted).toHaveBeenCalledTimes(1)
    })

    it('does not cancel a result navigation that finished its swap', () => {
        renderModalSearch()

        act(() => {
            modalSearchStore.getState().actions.openModal()
        })

        const result = document.createElement('a')
        result.setAttribute('data-search-result-index', '0')
        const aborted = jest.fn()
        const onAbort = (event: Event) => {
            if ((event as CustomEvent).detail?.elt === result) aborted()
        }
        document.body.addEventListener('htmx:abort', onAbort)

        act(() => {
            document.dispatchEvent(
                new CustomEvent('htmx:beforeSend', { detail: { elt: result } })
            )
            document.dispatchEvent(new CustomEvent('htmx:afterSwap'))
        })

        expect(modalSearchStore.getState().isOpen).toBe(false)
        document.body.removeEventListener('htmx:abort', onAbort)
        expect(aborted).not.toHaveBeenCalled()
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
