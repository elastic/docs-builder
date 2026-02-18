import '../../eui-icons-cache'
import { useHtmxContainer } from '../shared/htmx/useHtmxContainer'
import { useTheme } from '../shared/useTheme'
import { DeploymentInfo } from './DeploymentInfo'
import {
    EuiHeader,
    EuiHeaderLogo,
    EuiHeaderSectionItemButton,
    EuiIcon,
    EuiProvider,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
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
    const { euiTheme } = useEuiTheme()
    const containerRef = useRef<HTMLSpanElement>(null)
    useHtmxContainer(containerRef)
    const { theme, toggleTheme } = useTheme()

    return (
        <EuiProvider
            colorMode={theme}
            globalStyles={false}
            utilityClasses={false}
        >
            <EuiHeader
                css={css`
                    background-color: ${euiTheme.colors.primary};
                `}
                sections={[
                    {
                        items: [
                            <span ref={containerRef}>
                                <EuiHeaderLogo
                                    href={logoHref}
                                    css={css`
                                        & > span {
                                            color: var(--color-white);
                                        }
                                    `}
                                >
                                    {title}
                                </EuiHeaderLogo>
                            </span>,
                        ],
                    },
                    {
                        items: [
                            <EuiHeaderSectionItemButton
                                onClick={toggleTheme}
                                aria-label={
                                    theme === 'dark'
                                        ? 'Switch to light mode'
                                        : 'Switch to dark mode'
                                }
                            >
                                <EuiIcon
                                    type={theme === 'dark' ? 'sun' : 'moon'}
                                />
                            </EuiHeaderSectionItemButton>,
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
