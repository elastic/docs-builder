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
import { useModalActions, useModalMode } from './modal.store'
import { useCooldown } from './useCooldown'
import {
    EuiTabbedContent,
    EuiIcon,
    type EuiTabbedContentTab,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'
import { useMemo } from 'react'

export const SearchOrAskAiModal = React.memo(() => {
    const { euiTheme } = useEuiTheme()
    const modalMode = useModalMode()
    const { setModalMode } = useModalActions()

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

    const tabs: EuiTabbedContentTab[] = useMemo(
        () => [
            {
                id: 'search',
                name: 'Search',
                prepend: <EuiIcon type="search" />,
                content: <Search />,
            },
            {
                id: 'askAi',
                name: 'Ask AI',
                prepend: <EuiIcon type="sparkles" />,
                content: <Chat />,
            },
        ],
        []
    )

    const selectedTab = tabs.find((tab) => tab.id === modalMode) || tabs[0]

    return (
        <>
            <EuiTabbedContent
                css={css`
                    .euiTabs {
                        padding-inline: ${euiTheme.size.base};
                    }
                `}
                tabs={tabs}
                selectedTab={selectedTab}
                onTabClick={(tab) => setModalMode(tab.id as 'search' | 'askAi')}
            />
            {/*<ModalFooter />*/}
        </>
    )
})
