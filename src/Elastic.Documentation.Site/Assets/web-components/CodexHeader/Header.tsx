import '../../eui-icons-cache'
import { AskAiHeaderButton } from '../AskAi/AskAiHeaderButton'
import { NavigationSearch } from '../NavigationSearch/NavigationSearch'
import { useHtmxContainer } from '../shared/htmx/useHtmxContainer'
import { sharedQueryClient } from '../shared/queryClient'
import { EuiHeader, EuiHeaderLogo, EuiProvider } from '@elastic/eui'
import r2wc from '@r2wc/react-to-web-component'
import { QueryClientProvider } from '@tanstack/react-query'
import { useRef } from 'react'

interface Props {
    title: string
    logoHref: string
    githubRepository: string
    githubLink: string
    gitBranch: string
    gitCommit: string
    githubRef?: string
}

export const Header = ({ title, logoHref }: Props) => {
    const containerRef = useRef<HTMLSpanElement>(null)
    useHtmxContainer(containerRef)

    return (
        <QueryClientProvider client={sharedQueryClient}>
            <EuiProvider
                colorMode="dark"
                globalStyles={false}
                utilityClasses={false}
            >
                <EuiHeader
                    sections={[
                        {
                            items: [
                                <span ref={containerRef}>
                                    <EuiHeaderLogo href={logoHref}>
                                        {title}
                                    </EuiHeaderLogo>
                                </span>,
                            ],
                        },
                        {
                            items: [
                                <NavigationSearch
                                    size="s"
                                    placeholder="Search"
                                />,
                                <AskAiHeaderButton key="ask-ai" />,
                                // <EuiHeaderSectionItemButton key="theme">
                                //     <EuiIcon type="sun" />
                                // </EuiHeaderSectionItemButton>,
                            ],
                        },
                    ]}
                />
            </EuiProvider>
        </QueryClientProvider>
    )
}

customElements.define(
    'codex-header',
    r2wc(Header, {
        props: {
            title: 'string',
            logoHref: 'string',
            githubRepository: 'string',
            githubLink: 'string',
            gitBranch: 'string',
            gitCommit: 'string',
            githubRef: 'string',
        },
    })
)
