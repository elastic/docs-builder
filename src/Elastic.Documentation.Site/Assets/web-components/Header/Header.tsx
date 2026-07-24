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
    githubRepository?: string
    githubLink?: string
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
    /** When true the git remote belongs to the `elastic` GitHub organization. Controls whether the Elastic logo is shown as default. */
    elasticOrg?: boolean
    /** Custom header background CSS colour. Only used when branded=true; defaults to #000000. */
    headerBg?: string
    /** Custom icon image URL. When set (and branded=true), renders an <img> instead of the title text. */
    iconSrc?: string
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
    branded = false,
    elasticOrg = false,
    headerBg,
    iconSrc,
}: Props) => {
    const { euiTheme } = useEuiTheme()
    const containerRef = useRef<HTMLSpanElement>(null)
    useHtmxContainer(containerRef)

    const logoSection = branded ? (
        iconSrc ? (
            // Branded with icon — plain <a>, no HTMX (no containerRef)
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
                        height: 32px;
                        width: auto;
                    `}
                />
                {title}
            </a>
        ) : (
            // Branded without icon — title text only, no HTMX
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
        )
    ) : elasticOrg ? (
        // Default for elastic org: Elastic-branded logo (light-mode styling)
        <span ref={containerRef}>
            <EuiHeaderLogo
                href={logoHref}
                css={css`
                    padding-block: 7px;
                    height: auto;
                    line-height: normal;
                    border-radius: ${euiTheme.border.radius.small};
                    &:hover {
                        background: rgba(0, 0, 0, 0.06) !important;
                    }
                    & > span {
                        color: ${euiTheme.colors.textInk};
                    }
                `}
            >
                {title}
            </EuiHeaderLogo>
        </span>
    ) : (
        // Non-elastic org, no branding: title text only, no logo
        <span ref={containerRef}>
            <a
                href={logoHref}
                css={css`
                    display: inline-flex;
                    align-items: center;
                    padding: ${euiTheme.size.s};
                    font-weight: ${euiTheme.font.weight.bold};
                    color: ${euiTheme.colors.textInk};
                    text-decoration: none;
                    border-radius: ${euiTheme.border.radius.small};
                    &:hover {
                        background: rgba(0, 0, 0, 0.06);
                    }
                `}
            >
                {title}
            </a>
        </span>
    )

    const headerCss =
        branded && headerBg
            ? css`
                  background-color: ${headerBg};
              `
            : css`
                  background: linear-gradient(
                      to bottom,
                      #ffffff 0%,
                      #f5f7fa 100%
                  );
                  border-bottom: 1px solid ${euiTheme.colors.lightShade};
                  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.07);
              `

    return (
        <EuiProvider
            colorMode="light"
            globalStyles={false}
            utilityClasses={false}
        >
            <EuiHeader
                css={headerCss}
                sections={[
                    {
                        items: [logoSection],
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
            elasticOrg: 'boolean',
            headerBg: 'string',
            iconSrc: 'string',
        },
    })
)
