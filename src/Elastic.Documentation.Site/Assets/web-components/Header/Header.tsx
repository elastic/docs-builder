import '../../eui-icons-cache'
import { useHtmxContainer } from '../shared/htmx/useHtmxContainer'
import { DeploymentInfo } from './DeploymentInfo'
import { EuiHeader, EuiHeaderLogo, EuiProvider } from '@elastic/eui'
import r2wc from '@r2wc/react-to-web-component'
import { useRef } from 'react'

interface Props {
    title: string
    logoHref: string
    githubRepository: string
    githubLink: string
    gitBranch: string
    gitCommit: string
    /** Full ref from GitHub Actions (e.g. refs/pull/123/merge). */
    githubRef?: string
}

export const Header = ({
    title,
    logoHref,
    githubRepository,
    gitBranch,
    gitCommit,
    githubRef,
}: Props) => {
    const containerRef = useRef<HTMLSpanElement>(null)
    useHtmxContainer(containerRef)

    return (
        <EuiProvider
            colorMode="light"
            globalStyles={false}
            utilityClasses={false}
        >
            <EuiHeader
                theme="dark"
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
                            <DeploymentInfo
                                gitBranch={gitBranch}
                                gitCommit={gitCommit}
                                githubRepository={'elastic/' + githubRepository}
                                githubRef={githubRef}
                            />,
                        ],
                    },
                ]}
            />
        </EuiProvider>
    )
}

customElements.define(
    'elastic-docs-header',
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
