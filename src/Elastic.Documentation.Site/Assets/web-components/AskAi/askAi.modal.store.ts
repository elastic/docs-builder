import { persist, createJSONStorage } from 'zustand/middleware'
import { create } from 'zustand/react'

const DEFAULT_FLYOUT_WIDTH = 400

interface ModalState {
    isOpen: boolean
    flyoutWidth: number
    actions: {
        openModal: () => void
        closeModal: () => void
        toggleModal: () => void
        setFlyoutWidth: (width: number) => void
    }
}

// Close modal while preserving scroll position.
// When the flyout closes, focus is restored to a previously focused element,
// causing the browser to scroll that element into view. This can jump the page
// unexpectedly. We prevent this by intercepting any scroll events during close
// and immediately undoing them.
const closeWithScrollPreservation = (
    set: (state: Partial<ModalState>) => void
) => {
    const scrollY = window.scrollY
    // Intercept any scroll that happens during close and undo it
    const undoScroll = () => window.scrollTo(0, scrollY)
    window.addEventListener('scroll', undoScroll)
    set({ isOpen: false })
    // Clean up after React updates complete
    requestAnimationFrame(() => {
        requestAnimationFrame(() => {
            window.removeEventListener('scroll', undoScroll)
        })
    })
}

const askAiModalStore = create<ModalState>()(
    persist(
        (set) => ({
            isOpen: false,
            flyoutWidth: DEFAULT_FLYOUT_WIDTH,
            actions: {
                openModal: () => set({ isOpen: true }),
                closeModal: () => closeWithScrollPreservation(set),
                toggleModal: () => set((state) => ({ isOpen: !state.isOpen })),
                setFlyoutWidth: (width: number) => set({ flyoutWidth: width }),
            },
        }),
        {
            name: 'elastic-docs-ask-ai-state',
            version: 1,
            storage: createJSONStorage(() => localStorage),
            partialize: (state) => ({
                isOpen: state.isOpen,
                flyoutWidth: state.flyoutWidth,
                // Exclude actions (functions)
            }),
        }
    )
)

export const useAskAiModalIsOpen = () =>
    askAiModalStore((state) => state.isOpen)
export const useAskAiModalActions = () =>
    askAiModalStore((state) => state.actions)
export const useFlyoutWidth = () =>
    askAiModalStore((state) => state.flyoutWidth)

export { askAiModalStore }
