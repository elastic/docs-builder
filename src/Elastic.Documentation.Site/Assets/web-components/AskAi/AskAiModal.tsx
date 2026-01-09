import { Chat } from './Chat'
import { useAskAiCooldown, useAskAiCooldownActions } from './useAskAiCooldown'
import { useCooldown } from './useCooldown'
import * as React from 'react'

export const AskAiModal = React.memo(() => {
    // Manage cooldown countdowns at the modal level so they continue running
    const askAiCooldown = useAskAiCooldown()
    const { notifyCooldownFinished: notifyAskAiCooldownFinished } =
        useAskAiCooldownActions()

    useCooldown({
        domain: 'askAi',
        cooldown: askAiCooldown,
        onCooldownFinished: () => notifyAskAiCooldownFinished(),
    })

    return <Chat />
})
