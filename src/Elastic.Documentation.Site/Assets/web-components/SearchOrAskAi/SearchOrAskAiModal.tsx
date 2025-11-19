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
    EuiBetaBadge,
    EuiText,
    EuiLink,
    EuiTabbedContent,
    EuiIcon,
    EuiHorizontalRule,
    type EuiTabbedContentTab,
} from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'
import { useMemo } from 'react'

export const SearchOrAskAiModal = React.memo(() => {
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
                tabs={tabs}
                selectedTab={selectedTab}
                onTabClick={(tab) => setModalMode(tab.id as 'search' | 'askAi')}
            />
            <ModalFooter />
        </>
    )
})

const ModalFooter = () => {
    return (
        <>
            <EuiHorizontalRule margin="m" />
            <div
                css={css`
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    gap: calc(var(--spacing) * 2);
                `}
            >
                <EuiBetaBadge
                    css={css`
                        display: inline-flex;
                    `}
                    label="Alpha"
                    color="accent"
                    tooltipContent="This feature is in private preview and is only enabled if you are in Elastic's Global VPN."
                />

                <EuiText color="subdued" size="s">
                    This feature is in private preview (alpha).{' '}
                    <EuiLink
                        target="_blank"
                        rel="noopener noreferrer"
                        href="https://github.com/elastic/docs-eng-team/issues/new?template=search-or-ask-ai-feedback.yml"
                    >
                        Got feedback? We'd love to hear it!
                    </EuiLink>
                </EuiText>
            </div>
        </>
    )
}
