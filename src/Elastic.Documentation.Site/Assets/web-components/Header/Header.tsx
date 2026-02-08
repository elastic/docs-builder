import { useHtmxLink } from '../shared/htmx/useHtmxLink'
import githubSvg from './GitHub_Invertocat_Black.svg'
import {
    EuiBadge,
    EuiHeader,
    EuiHeaderLink,
    EuiHeaderLinks,
    EuiHeaderLogo,
    EuiHeaderSectionItem,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
// import { css } from '@emotion/react'
import r2wc from '@r2wc/react-to-web-component'

interface Props {
    title: string
    logoHref: string
    githubRepository: string
    githubLink: string
    gitBranch: string
    gitCommit: string
}

export const Header = ({
    title,
    logoHref,
    githubRepository,
    githubLink,
    gitCommit,
    gitBranch,
}: Props) => {
    const { euiTheme } = useEuiTheme()
    const logoLink = useHtmxLink(logoHref)
    return (
        <EuiHeader
            theme="dark"
            sections={[
                {
                    items: [
                        <a
                            key="logo"
                            ref={logoLink.ref}
                            href={logoLink.href}
                            css={css`
                                text-decoration: none;
                                color: inherit;
                            `}
                        >
                            <EuiHeaderLogo>{title}</EuiHeaderLogo>
                        </a>,
                        // <EuiHeaderLinks aria-label="App navigation dark theme example">
                        //     <EuiHeaderLink isActive>Docs</EuiHeaderLink>
                        //     <EuiHeaderLink>Code</EuiHeaderLink>
                        //     <EuiHeaderLink iconType="help"> Help</EuiHeaderLink>
                        // </EuiHeaderLinks>,
                    ],
                },
                {
                    items: [
                        <EuiHeaderSectionItem>
                            <EuiHeaderLinks>
                                <EuiHeaderLink
                                    iconType={githubSvg}
                                    href={githubLink}
                                    target="_blank"
                                    rel="noopener"
                                >
                                    elastic/{githubRepository}@{gitBranch}
                                </EuiHeaderLink>
                            </EuiHeaderLinks>
                        </EuiHeaderSectionItem>,
                        <EuiBadge
                            iconType="dotInCircle"
                            css={css`
                                margin-inline: ${euiTheme.size.s};
                            `}
                        >
                            {gitCommit}
                        </EuiBadge>,
                        // <EuiHeaderSectionItemButton
                        // aria-controls="headerFlyoutNewsFeed"
                        // aria-haspopup="true"
                        // aria-label={'Alerts feed: Updates available'}
                        // // onClick={() => showFlyout()}
                        // notification={true}
                        // >
                        // <EuiIcon type="bell" />
                        // </EuiHeaderSectionItemButton>
                    ],
                },
            ]}
        />
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
        },
    })
)
