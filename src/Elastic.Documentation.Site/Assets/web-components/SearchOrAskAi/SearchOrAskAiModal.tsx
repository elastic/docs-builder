import { Chat } from './AskAi/Chat'
import { Search } from './Search/Search'
import { useModalActions, useModalMode } from './modal.store'
import {
    EuiBetaBadge,
    EuiText,
    EuiLink,
    EuiTabbedContent,
    EuiIcon,
    type EuiTabbedContentTab,
} from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'
import { useMemo } from 'react'

export const SearchOrAskAiModal = React.memo(() => {
    const modalMode = useModalMode()
    const { setModalMode } = useModalActions()

    const tabs: EuiTabbedContentTab[] = useMemo(
        () => [
            {
                id: 'search',
                name: 'Search',
                prepend: <EuiIcon type="search" />,
                content: <Search />
                ,
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
        <div
            css={css`
                display: flex;
                align-items: center;
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
    )
}
