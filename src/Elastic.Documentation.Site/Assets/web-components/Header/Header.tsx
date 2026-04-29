import '../../eui-icons-cache'
import { useHtmxContainer } from '../shared/htmx/useHtmxContainer'
import { DeploymentInfo, headerButtonCss } from './DeploymentInfo'
import {
    EuiHeader,
    EuiHeaderLogo,
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
    /** When true, deployment info is hidden (not relevant in air-gapped environments). */
    airGapped?: boolean
}

export const Header = ({
    title,
    logoHref,
    githubRepository,
    githubLink,
    gitBranch,
    gitCommit,
    githubRef,
    airGapped = false,
}: Props) => {
    const { euiTheme } = useEuiTheme()
    const containerRef = useRef<HTMLSpanElement>(null)
    useHtmxContainer(containerRef)

    return (
        <EuiProvider
            colorMode="light"
            globalStyles={false}
            utilityClasses={false}
        >
            <EuiHeader
                css={css`
                    background: linear-gradient(
                        to bottom,
                        #ffffff 0%,
                        #f5f7fa 100%
                    );
                    border-bottom: 1px solid ${euiTheme.colors.lightShade};
                    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.07);
                `}
                sections={[
                    {
                        items: [
                            <span ref={containerRef}>
                                <EuiHeaderLogo
                                    href={logoHref}
                                    css={css`
                                        padding-block: 7px;
                                        height: auto;
                                        line-height: normal;
                                        border-radius: ${euiTheme.border.radius
                                            .small};
                                        &:hover {
                                            background: rgba(
                                                0,
                                                0,
                                                0,
                                                0.06
                                            ) !important;
                                        }
                                        & > span {
                                            color: ${euiTheme.colors.ink};
                                        }
                                    `}
                                >
                                    {title}
                                </EuiHeaderLogo>
                            </span>,
                        ],
                    },
                    ...(!airGapped
                        ? [
                              {
                                  items: [
                                      ...(githubLink
                                          ? [
                                                <a
                                                    href={githubLink}
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                    css={css`
                                                        ${headerButtonCss(
                                                            euiTheme
                                                        )};
                                                        margin-inline: ${euiTheme
                                                            .size.s};
                                                    `}
                                                >
                                                    <EuiIcon
                                                        type="logoGithub"
                                                        color="inherit"
                                                    />
                                                    GitHub
                                                </a>,
                                            ]
                                          : []),
                                      <DeploymentInfo
                                          gitBranch={gitBranch}
                                          gitCommit={gitCommit}
                                          githubRepository={
                                              'elastic/' + githubRepository
                                          }
                                          githubRef={githubRef}
                                      />,
                                  ],
                              },
                          ]
                        : []),
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
            airGapped: 'boolean',
        },
    })
)
