import {
    EuiAvatar,
    EuiBadge,
    EuiHeader,
    // EuiHeaderLink,
    // EuiHeaderLinks,
    EuiHeaderLogo,
    EuiHeaderSectionItemButton,
    EuiIcon,
    useEuiTheme,
} from '@elastic/eui'
// import { css } from '@emotion/react'
import r2wc from '@r2wc/react-to-web-component'

interface Props {
    title: string
}

export const Header = ({ title }: Props) => {
    const { euiTheme } = useEuiTheme()
    return (
        <EuiHeader
            theme="dark"
            sections={[
                {
                    items: [
                        <EuiHeaderLogo>{title}</EuiHeaderLogo>,
                        // <EuiHeaderLinks aria-label="App navigation dark theme example">
                        //     <EuiHeaderLink isActive>Docs</EuiHeaderLink>
                        //     <EuiHeaderLink>Code</EuiHeaderLink>
                        //     <EuiHeaderLink iconType="help"> Help</EuiHeaderLink>
                        // </EuiHeaderLinks>,
                    ],
                },
                {
                    items: [
                        <EuiBadge
                            color={euiTheme.colors.darkestShade}
                            iconType="arrowDown"
                            iconSide="right"
                        >
                            Production logs
                        </EuiBadge>,
                        <EuiHeaderSectionItemButton
                            aria-label="2 Notifications"
                            notification={'2'}
                        >
                            <EuiIcon type="cheer" size="m" />
                        </EuiHeaderSectionItemButton>,
                        <EuiHeaderSectionItemButton aria-label="Account menu">
                            <EuiAvatar name="John Username" size="s" />
                        </EuiHeaderSectionItemButton>,
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
        },
    })
)
