import {
    EuiButton,
    EuiContextMenu,
    EuiFlexGroup,
    EuiFlexItem,
    EuiIcon,
    EuiPopover,
    EuiText,
    useGeneratedHtmlId,
} from '@elastic/eui'
import { icon as EuiIconVisualizeApp } from '@elastic/eui/es/components/icon/assets/app_visualize'
import { icon as EuiIconArrowDown } from '@elastic/eui/es/components/icon/assets/arrow_down'
import { icon as EuiIconArrowLeft } from '@elastic/eui/es/components/icon/assets/arrow_left'
import { icon as EuiIconArrowRight } from '@elastic/eui/es/components/icon/assets/arrow_right'
import { icon as EuiIconCheck } from '@elastic/eui/es/components/icon/assets/check'
import { icon as EuiIconDocument } from '@elastic/eui/es/components/icon/assets/document'
import { icon as EuiIconSearch } from '@elastic/eui/es/components/icon/assets/search'
import { icon as EuiIconTrash } from '@elastic/eui/es/components/icon/assets/trash'
import { icon as EuiIconUser } from '@elastic/eui/es/components/icon/assets/user'
import { icon as EuiIconWrench } from '@elastic/eui/es/components/icon/assets/wrench'
import { appendIconComponentCache } from '@elastic/eui/es/components/icon/icon'
import {
    EuiContextMenuPanelDescriptor,
    EuiContextMenuPanelItemDescriptor,
} from '@elastic/eui/src/components/context_menu/context_menu'
import { css } from '@emotion/react'
import r2wc from '@r2wc/react-to-web-component'
import * as React from 'react'
import { useState } from 'react'

// One or more icons are passed in as an object of iconKey (string): IconComponent
appendIconComponentCache({
    arrowDown: EuiIconArrowDown,
    arrowLeft: EuiIconArrowLeft,
    arrowRight: EuiIconArrowRight,
    document: EuiIconDocument,
    search: EuiIconSearch,
    trash: EuiIconTrash,
    user: EuiIconUser,
    wrench: EuiIconWrench,
    visualizeApp: EuiIconVisualizeApp,
    check: EuiIconCheck,
})

type VersionDropdownItem = {
    name: string
    href?: string
    children?: VersionDropdownItem[]
}

type VersionDropdownProps = {
    items: VersionDropdownItem[]
}

const VersionDropdown = ({ items }: VersionDropdownProps) => {
    const [isPopoverOpen, setPopover] = useState(false)

    const contextMenuPopoverId = useGeneratedHtmlId({
        prefix: 'contextMenuPopover',
    })

    const onButtonClick = () => {
        setPopover(!isPopoverOpen)
    }

    const closePopover = () => {
        setPopover(false)
    }

    const convertItems = (
        items: VersionDropdownItem[]
    ): EuiContextMenuPanelItemDescriptor[] => {
        return items.map((item) => {
            return {
                name: item.name,
                href: item.href,
            }
        })
    }

    const convertToPanels = (
        items: VersionDropdownItem[]
    ): EuiContextMenuPanelDescriptor[] => {
        return items.flatMap((item, index) => {
            if (item.children == null) {
                return []
            } else {
                return {
                    id: index + 1,
                    title: item.name,
                    initialFocusedItemIndex: 0,
                    width: WIDTH,
                    size: 's',
                    items: item.children ? convertItems(item.children) : [],
                }
            }
        })
    }

    const WIDTH = 175
    
    const topLevelItems = items.map((item, index) => {
        return {
            name: item.name,
            panel: item.children?.length ? index + 1 : undefined,
            href: item.href,
        }
    })
    
    const subpanels = convertToPanels(items)

    const panels: EuiContextMenuPanelDescriptor[] = [
        {
            id: 0,
            title: (
                <EuiFlexGroup gutterSize="s" alignItems="center">
                    <EuiFlexItem grow={0}>
                        <EuiIcon type="check" />
                    </EuiFlexItem>
                    <EuiFlexItem grow={1}>Current (9.0+)</EuiFlexItem>
                </EuiFlexGroup>
            ),
            width: WIDTH,
            size: 's',
            items: [
                ...topLevelItems,
                {
                    name: 'All versions',
                    href: 'https://elastic.co',
                },
            ],
        },
        ...subpanels,
    ]
    
    // const panels = [
    //     {
    //         id: 0,
    //         title: <EuiFlexGroup gutterSize="s" alignItems="center">
    //                     <EuiFlexItem grow={0}>
    //                         <EuiIcon type="check" />
    //                     </EuiFlexItem>
    //                     <EuiFlexItem grow={1}>
    //                         Current (9.0+)
    //                     </EuiFlexItem>
    //         </EuiFlexGroup>,
    //         width: WIDTH,
    //         size: 's',
    //         items: [
    //             {
    //                 name: '8.x',
    //                 panel: 1,
    //             },
    //             {
    //                 name: '7.17',
    //                 href: 'https://elastic.co'
    //             },
    //             {
    //                 name: 'All versions',
    //                 href: 'https://elastic.co'
    //             },
    //         ],
    //     },
    //     {
    //         id: 1,
    //         initialFocusedItemIndex: 1,
    //         title: '8.x',
    //         width: WIDTH,
    //         items: [
    //             {
    //                 name: <EuiText size="xs">8.16</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.17</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.16</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.15</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.14</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.13</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.12</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.11</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.10</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.9</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.8</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.7</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.6</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.5</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.4</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.3</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.2</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.1</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //             {
    //                 name: <EuiText size="xs">8.0</EuiText>,
    //                 href: 'https://elastic.co',
    //             },
    //         ],
    //     },
    // ];

    const button = (
        <EuiButton
            iconType="arrowDown"
            iconSide="right"
            onClick={onButtonClick}
            size="s"
            color="text"
            style={{ borderRadius: 9999 }}
        >
            <EuiText
                size="xs"
                css={css`
                    font-weight: 600;
                    font-size: 0.875rem;
                `}
            >
                Current (9.0+)
            </EuiText>
        </EuiButton>
    )

    return (
        <EuiPopover
            id={contextMenuPopoverId}
            button={button}
            isOpen={isPopoverOpen}
            closePopover={closePopover}
            panelPaddingSize="none"
            anchorPosition="downLeft"
            repositionOnScroll={true}
        >
            <EuiContextMenu initialPanelId={0} size="s" panels={panels} />
        </EuiPopover>
    )
}

customElements.define(
    'version-dropdown',
    r2wc(VersionDropdown, {
        props: {
            items: 'json',
        },
    })
)
