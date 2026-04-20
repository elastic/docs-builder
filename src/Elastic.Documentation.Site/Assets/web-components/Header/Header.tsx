import '../../eui-icons-cache'
import { useHtmxContainer } from '../shared/htmx/useHtmxContainer'
import { DeploymentInfo } from './DeploymentInfo'
import {
    EuiHeader,
    EuiHeaderLogo,
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
    /**
     * When true the docset has `branding` configured: suppresses the Elastic logo and
     * uses a custom background. The Razor view always passes this explicitly so the
     * component does not have to infer branding state from other optional props.
     */
    branded?: boolean
    /** Custom header background CSS colour. Only used when branded=true; defaults to #000000. */
    headerBg?: string
    /** Custom icon image URL. When set (and branded=true), renders an <img> instead of the title text. */
    iconSrc?: string
}

export const Header = ({
    title,
    logoHref,
    githubRepository,
    gitBranch,
    gitCommit,
    githubRef,
    airGapped = false,
    branded = false,
    headerBg,
    iconSrc,
}: Props) => {
    const { euiTheme } = useEuiTheme()
    const containerRef = useRef<HTMLSpanElement>(null)
    useHtmxContainer(containerRef)

    const bgColor = branded ? headerBg || '#000000' : euiTheme.colors.primary

    const logoSection = branded ? (
        iconSrc ? (
            <span ref={containerRef}>
                <a
                    href={logoHref}
                    css={css`
                        display: inline-flex;
                        align-items: center;
                        gap: ${euiTheme.size.s};
                        color: var(--color-white);
                        text-decoration: none;
                        padding: ${euiTheme.size.s};
                    `}
                >
                    <img
                        src={iconSrc}
                        alt={title}
                        css={css`
                            height: 24px;
                            width: auto;
                        `}
                    />
                    {title}
                </a>
            </span>
        ) : (
            // Branding configured but no icon — title text only, no Elastic logo
            <span ref={containerRef}>
                <a
                    href={logoHref}
                    css={css`
                        display: inline-flex;
                        align-items: center;
                        color: var(--color-white);
                        text-decoration: none;
                        padding: ${euiTheme.size.s};
                        font-weight: ${euiTheme.font.weight.bold};
                    `}
                >
                    {title}
                </a>
            </span>
        )
    ) : (
        // Default: Elastic-branded logo
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
        </span>
    )

    return (
        <EuiProvider
            colorMode="light"
            globalStyles={false}
            utilityClasses={false}
        >
            <EuiHeader
                css={css`
                    background-color: ${bgColor};
                `}
                sections={[
                    {
                        items: [logoSection],
                    },
                    ...(!airGapped
                        ? [
                              {
                                  items: [
                                      <DeploymentInfo
                                          gitBranch={gitBranch}
                                          gitCommit={gitCommit}
                                          githubRepository={githubRepository}
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
            branded: 'boolean',
            headerBg: 'string',
            iconSrc: 'string',
        },
    })
)
