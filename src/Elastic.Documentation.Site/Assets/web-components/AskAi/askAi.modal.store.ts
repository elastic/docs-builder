import { create } from 'zustand/react'

interface ModalState {
    isOpen: boolean
    actions: {
        openModal: () => void
        closeModal: () => void
        toggleModal: () => void
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

const askAiModalStore = create<ModalState>((set) => ({
    isOpen: false,
    actions: {
        openModal: () => set({ isOpen: true }),
        closeModal: () => closeWithScrollPreservation(set),
        toggleModal: () => set((state) => ({ isOpen: !state.isOpen })),
    },
}))

export const useAskAiModalIsOpen = () =>
    askAiModalStore((state) => state.isOpen)
export const useAskAiModalActions = () =>
    askAiModalStore((state) => state.actions)

export { askAiModalStore }
