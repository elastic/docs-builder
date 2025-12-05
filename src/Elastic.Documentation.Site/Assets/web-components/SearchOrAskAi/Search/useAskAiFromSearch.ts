import { useChatActions } from '../AskAi/chat.store'
import { useIsAskAiCooldownActive } from '../AskAi/useAskAiCooldown'
import { useModalActions } from '../modal.store'
import { useSearchTerm } from './search.store'
import { useCallback } from 'react'

export const useAskAiFromSearch = () => {
    const searchTerm = useSearchTerm()
    const isAskAiCooldownActive = useIsAskAiCooldownActive()
    const { submitQuestion, clearChat } = useChatActions()
    const { setModalMode } = useModalActions()

    const askAi = useCallback(() => {
        const trimmedSearchTerm = searchTerm.trim()
        if (isAskAiCooldownActive || trimmedSearchTerm === '') {
            return
        }
        clearChat()
        submitQuestion('Tell me more about ' + trimmedSearchTerm)
        setModalMode('askAi')
    }, [
        searchTerm,
        isAskAiCooldownActive,
        clearChat,
        submitQuestion,
        setModalMode,
    ])

    return { askAi, isDisabled: isAskAiCooldownActive }
}
