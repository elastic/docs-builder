'strict'

import {
    EuiButton,
    EuiContextMenu,
    EuiFlexGroup,
    EuiFlexItem,
    EuiIcon,
    EuiPopover,
    EuiText,
    EuiPanel,
    EuiLink,
    useEuiOverflowScroll,
    useGeneratedHtmlId,
    useEuiTheme,
    useEuiFontSize,
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
    disabled: boolean
    children?: VersionDropdownItem[]
}

type VersionDropdownProps = {
    currentVersion?: string
    allVersionsUrl?: string
    items?: VersionDropdownItem[]
}

const VersionDropdown = ({
    allVersionsUrl,
    currentVersion,
    items,
}: VersionDropdownProps) => {
    const [isPopoverOpen, setPopover] = useState(false)
    const { euiTheme } = useEuiTheme()

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
                disabled: item.disabled,
            }
        })
    }

    const convertToPanels = (
        items: VersionDropdownItem[]
    ): EuiContextMenuPanelDescriptor[] => {
        return items == null
            ? []
            : items.flatMap((item, index) => {
                  if (item.children == null) {
                      return []
                  } else {
                      return {
                          id: index + 1,
                          title: item.name,
                          initialFocusedItemIndex: 0,
                          width: WIDTH,
                          disabled: item.disabled,
                          size: 's',
                          items: item.children
                              ? convertItems(item.children)
                              : [],
                      }
                  }
              })
    }

    const WIDTH = 175

    const topLevelItems = () =>
        items.map((item, index) => {
            return {
                name: item.name,
                panel: item.children?.length ? index + 1 : undefined,
                href: item.href,
                disabled: item.disabled,
            }
        })

    const subpanels = () => convertToPanels(items)

    const panels = (): EuiContextMenuPanelDescriptor[] => [
        {
            id: 0,
            title: (
                <EuiFlexGroup gutterSize="s" alignItems="center">
                    <EuiFlexItem grow={0}>
                        <EuiIcon type="check" size="s" />
                    </EuiFlexItem>
                    <EuiFlexItem grow={1}>
                        <EuiText size="s">{currentVersion}</EuiText>
                    </EuiFlexItem>
                </EuiFlexGroup>
            ),
            width: WIDTH,
            size: 's',
            items: [
                ...(items == null
                    ? [
                          {
                              renderItem: () => (
                                  <EuiPanel paddingSize="s" hasShadow={false}>
                                      <EuiText size="xs" color="subdued">
                                          There are no other versions available
                                          for this page.
                                      </EuiText>
                                  </EuiPanel>
                              ),
                          },
                      ]
                    : topLevelItems()),
                ...(items?.length === 0
                    ? [
                          {
                              renderItem: () => (
                                  <EuiPanel paddingSize="s" hasShadow={false}>
                                      <EuiText size="xs" color="subdued">
                                          This page was fully migrated to the
                                          current version.
                                      </EuiText>
                                  </EuiPanel>
                              ),
                          },
                      ]
                    : []),
                ...(allVersionsUrl != null
                    ? [
                          {
                              renderItem: () => (
                                  <EuiPanel
                                      css={css`
                                          border-top: 1px solid
                                              ${euiTheme.border.color};
                                          padding: ${euiTheme.size.s};
                                      `}
                                  >
                                      <EuiLink
                                          href="/docs/versions"
                                          color="text"
                                      >
                                          <EuiText size="s">
                                              View other versions
                                          </EuiText>
                                      </EuiLink>
                                  </EuiPanel>
                              ),
                          },
                      ]
                    : []),
            ],
        },
        ...(items != null ? subpanels() : []),
    ]

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
                css={css`
                    font-weight: ${euiTheme.font.weight.bold};
                    font-size: ${useEuiFontSize('xs').fontSize};
                `}
            >
                Current version ({currentVersion})
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
            anchorPosition="downRight"
            repositionOnScroll={true}
        >
            <EuiContextMenu
                initialPanelId={0}
                size="s"
                panels={panels()}
                css={css`
                    max-height: 70vh;
                    // This is needed because the CSS reset we are using
                    // is probably not fully compatible with the EUI
                    button {
                        cursor: pointer;
                        &:disabled {
                            cursor: default;
                        }
                    }
                    .euiContextMenuPanel__title {
                        position: sticky;
                        top: 0;
                        // !important because clicking on the title
                        // makes the background transparent
                        // and you unexpectedly see the items behind it.
                        background-color: ${euiTheme.colors
                            .backgroundBasePlain} !important;
                    }
                    ${useEuiOverflowScroll('y')}
                `}
            />
        </EuiPopover>
    )
}

customElements.define(
    'version-dropdown',
    r2wc(VersionDropdown, {
        props: {
            items: 'json',
            currentVersion: 'string',
            allVersionsUrl: 'string',
        },
    })
)
