import { Chat } from './AskAi/Chat'
import {
    useAskAiCooldown,
    useAskAiCooldownActions,
} from './AskAi/useAskAiCooldown'
import { Search } from './Search/Search'
import {
    useSearchCooldown,
    useSearchCooldownActions,
} from './Search/useSearchCooldown'
import { useModalMode } from './modal.store'
import { useCooldown } from './useCooldown'
import * as React from 'react'

export const SearchOrAskAiModal = React.memo(() => {
    const modalMode = useModalMode()

    // Manage cooldown countdowns at the modal level so they continue running when switching tabs
    const searchCooldown = useSearchCooldown()
    const { notifyCooldownFinished: notifySearchCooldownFinished } =
        useSearchCooldownActions()
    const askAiCooldown = useAskAiCooldown()
    const { notifyCooldownFinished: notifyAskAiCooldownFinished } =
        useAskAiCooldownActions()

    useCooldown({
        domain: 'search',
        cooldown: searchCooldown,
        onCooldownFinished: () => notifySearchCooldownFinished(),
    })

    useCooldown({
        domain: 'askAi',
        cooldown: askAiCooldown,
        onCooldownFinished: () => notifyAskAiCooldownFinished(),
    })
    return modalMode === 'search' ? <Search /> : <Chat />
})
