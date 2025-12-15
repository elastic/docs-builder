'use strict'

import '../eui-icons-cache'
import { EuiPopover, useGeneratedHtmlId } from '@elastic/eui'
import { css } from '@emotion/react'
import r2wc from '@r2wc/react-to-web-component'
import * as React from 'react'
import { useState, useRef, useEffect, useCallback } from 'react'

type PopoverAvailabilityItem = {
    text: string
    lifecycleDescription?: string
}

type PopoverData = {
    productDescription?: string
    availabilityItems: PopoverAvailabilityItem[]
    additionalInfo?: string
    showVersionNote: boolean
    versionNote?: string
}

type AppliesToPopoverProps = {
    badgeKey?: string
    badgeLifecycleText?: string
    badgeVersion?: string
    lifecycleClass?: string
    lifecycleName?: string
    showLifecycleName?: boolean
    showVersion?: boolean
    hasMultipleLifecycles?: boolean
    popoverData?: PopoverData
    showPopover?: boolean
    isInline?: boolean
}

const AppliesToPopover = ({
    badgeKey,
    badgeLifecycleText,
    badgeVersion,
    lifecycleClass,
    lifecycleName,
    showLifecycleName,
    showVersion,
    hasMultipleLifecycles,
    popoverData,
    showPopover = true,
    isInline = false,
}: AppliesToPopoverProps) => {
    const [isOpen, setIsOpen] = useState(false)
    const [isPinned, setIsPinned] = useState(false)
    const [openItems, setOpenItems] = useState<Set<number>>(new Set())
    const popoverId = useGeneratedHtmlId({ prefix: 'appliesToPopover' })
    const contentRef = useRef<HTMLDivElement>(null)
    const badgeRef = useRef<HTMLSpanElement>(null)
    const hoverTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

    const hasPopoverContent =
        popoverData &&
        (popoverData.productDescription ||
            popoverData.availabilityItems.length > 0 ||
            popoverData.additionalInfo ||
            popoverData.showVersionNote)

    const openPopover = useCallback(() => {
        if (showPopover && hasPopoverContent) {
            setIsOpen(true)
        }
    }, [showPopover, hasPopoverContent])

    const closePopover = useCallback(() => {
        if (!isPinned) {
            setIsOpen(false)
        }
    }, [isPinned])

    const handleClick = useCallback(() => {
        if (showPopover && hasPopoverContent) {
            if (isPinned) {
                // If already pinned, unpin and close
                setIsPinned(false)
                setIsOpen(false)
            } else {
                // Pin the popover open
                setIsPinned(true)
                setIsOpen(true)
            }
        }
    }, [showPopover, hasPopoverContent, isPinned])

    const toggleItem = useCallback((index: number, e: React.MouseEvent) => {
        e.stopPropagation()
        setOpenItems((prev) => {
            const next = new Set(prev)
            if (next.has(index)) {
                next.delete(index)
            } else {
                next.add(index)
            }
            return next
        })
    }, [])

    const handleClosePopover = useCallback(() => {
        setIsPinned(false)
        setIsOpen(false)
    }, [])

    const handleMouseEnter = useCallback(() => {
        // Clear any pending close timeout
        if (hoverTimeoutRef.current) {
            clearTimeout(hoverTimeoutRef.current)
            hoverTimeoutRef.current = null
        }
        openPopover()
    }, [openPopover])

    const handleMouseLeave = useCallback(() => {
        // Small delay before closing to allow moving to the popover content
        hoverTimeoutRef.current = setTimeout(() => {
            closePopover()
        }, 100)
    }, [closePopover])

    // Cleanup timeout on unmount
    useEffect(() => {
        return () => {
            if (hoverTimeoutRef.current) {
                clearTimeout(hoverTimeoutRef.current)
            }
        }
    }, [])

    // Close popover when badge becomes hidden (e.g., parent details element collapses)
    useEffect(() => {
        if (!badgeRef.current || !isOpen) return

        const observer = new IntersectionObserver(
            (entries) => {
                entries.forEach((entry) => {
                    // If badge is no longer visible, close the popover
                    if (!entry.isIntersecting) {
                        setIsPinned(false)
                        setIsOpen(false)
                    }
                })
            },
            { threshold: 0 }
        )

        observer.observe(badgeRef.current)

        return () => {
            observer.disconnect()
        }
    }, [isOpen])

    // Reset open items when popover closes
    useEffect(() => {
        if (!isOpen) {
            setOpenItems(new Set())
        }
    }, [isOpen])

    const showSeparator =
        badgeKey && (showLifecycleName || showVersion || badgeLifecycleText)

    const badgeButton = (
        <span
            ref={badgeRef}
            className={`applicable-info${showPopover && hasPopoverContent ? ' applicable-info--clickable' : ''}${isPinned ? ' applicable-info--pinned' : ''}`}
            onClick={handleClick}
            onMouseEnter={handleMouseEnter}
            onMouseLeave={handleMouseLeave}
            role={showPopover && hasPopoverContent ? 'button' : undefined}
            tabIndex={showPopover && hasPopoverContent ? 0 : undefined}
            onKeyDown={(e) => {
                if (
                    showPopover &&
                    hasPopoverContent &&
                    (e.key === 'Enter' || e.key === ' ')
                ) {
                    e.preventDefault()
                    handleClick()
                }
            }}
        >
            <span className="applicable-name">{badgeKey}</span>

            {showSeparator && <span className="applicable-separator"></span>}

            <span
                className={`applicable-meta applicable-meta-${lifecycleClass}`}
            >
                {showLifecycleName && (
                    <span
                        className={`applicable-lifecycle applicable-lifecycle-${lifecycleClass}`}
                    >
                        {lifecycleName}
                    </span>
                )}
                {showVersion ? (
                    <span
                        className={`applicable-version applicable-version-${lifecycleClass}`}
                    >
                        {badgeVersion}
                    </span>
                ) : (
                    badgeLifecycleText
                )}
                {hasMultipleLifecycles && (
                    <span className="applicable-ellipsis">
                        <span className="applicable-ellipsis__dot"></span>
                        <span className="applicable-ellipsis__dot"></span>
                        <span className="applicable-ellipsis__dot"></span>
                    </span>
                )}
            </span>
        </span>
    )

    if (!showPopover || !hasPopoverContent) {
        return badgeButton
    }

    const renderAvailabilityItem = (
        item: PopoverAvailabilityItem,
        index: number
    ) => {
        const isItemOpen = openItems.has(index)

        if (item.lifecycleDescription) {
            return (
                <div
                    key={index}
                    css={css`
                        margin: 0 0 4px 0;
                        border: none;
                        background: none;
                    `}
                >
                    <div
                        onClick={(e) => toggleItem(index, e)}
                        css={css`
                            display: flex;
                            align-items: center;
                            cursor: pointer;
                            padding: 4px 0;
                            color: var(--color-blue-elastic, #0077cc);
                            font-weight: 500;

                            &::before {
                                content: '';
                                display: inline-block;
                                width: 6px;
                                height: 6px;
                                border-right: 2px solid currentColor;
                                border-bottom: 2px solid currentColor;
                                transform: ${isItemOpen
                                    ? 'rotate(45deg)'
                                    : 'rotate(-45deg)'};
                                margin-right: 8px;
                                transition: transform 0.15s ease;
                            }

                            &:hover {
                                color: var(--color-blue-hover, #005fa3);
                            }
                        `}
                    >
                        <span
                            css={css`
                                flex: 1;
                            `}
                        >
                            {item.text}
                        </span>
                    </div>
                    {isItemOpen && (
                        <p
                            css={css`
                                margin: 4px 0 8px 16px;
                                padding: 8px 12px;
                                background: var(--color-grey-5, #f5f7fa);
                                border-radius: 4px;
                                font-size: 13px;
                                color: var(--color-grey-80, #343741);
                                line-height: 1.5;
                            `}
                        >
                            {item.lifecycleDescription}
                        </p>
                    )}
                </div>
            )
        }

        // Simple item without collapsible content
        return (
            <p
                key={index}
                css={css`
                    margin: 0 0 4px 0;
                    padding: 4px 0 4px 16px;
                    color: var(--color-grey-80, #343741);
                `}
            >
                {item.text}
            </p>
        )
    }

    return (
        <EuiPopover
            id={popoverId}
            button={badgeButton}
            isOpen={isOpen}
            closePopover={handleClosePopover}
            panelPaddingSize="none"
            anchorPosition="downLeft"
            repositionOnScroll={true}
            css={css`
                display: ${isInline ? 'inline-flex' : 'flex'};
                vertical-align: ${isInline ? 'bottom' : 'baseline'};
            `}
            panelProps={{
                onMouseEnter: handleMouseEnter,
                onMouseLeave: handleMouseLeave,
                css: css`
                    max-width: 420px;
                    z-index: 40 !important; /* Show below the main header if needed */

                    /* Remove all focus styling from panel */
                    &,
                    &:focus,
                    &:focus-visible,
                    &:focus-within {
                        outline: none !important;
                        box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1) !important;
                    }

                    .euiPopover__panelArrow {
                        display: none;
                    }
                `,
            }}
        >
            <div
                ref={contentRef}
                css={css`
                    padding: 16px;
                    font-size: 14px;
                    line-height: 1.5;
                    color: var(--color-grey-100, #1a1c21);
                `}
            >
                {/* Product description */}
                {popoverData?.productDescription && (
                    <p
                        css={css`
                            margin: 0 0 16px 0;
                            color: var(--color-grey-80, #343741);

                            strong {
                                display: inline;
                                font-weight: 700;
                                color: var(--color-grey-100, #1a1c21);
                            }
                        `}
                        dangerouslySetInnerHTML={{
                            __html: popoverData.productDescription,
                        }}
                    />
                )}

                {/* Availability section */}
                {popoverData && popoverData.availabilityItems.length > 0 && (
                    <>
                        <p
                            css={css`
                                margin: 0 0 4px 0;
                            `}
                        >
                            <strong
                                css={css`
                                    display: inline;
                                    font-weight: 700;
                                    font-size: 15px;
                                    color: var(--color-grey-100, #1a1c21);
                                `}
                            >
                                Availability
                            </strong>
                        </p>
                        <p
                            css={css`
                                margin: 0 0 8px 0;
                                color: var(--color-grey-70, #535966);
                                font-size: 13px;
                            `}
                        >
                            The functionality described here is:
                        </p>
                        {popoverData.availabilityItems.map((item, index) =>
                            renderAvailabilityItem(item, index)
                        )}
                    </>
                )}

                {/* Additional availability info */}
                {popoverData?.additionalInfo && (
                    <p
                        css={css`
                            margin: 12px 0 0 0;
                            padding-top: 12px;
                            border-top: 1px solid var(--color-grey-15, #e0e5ee);
                            color: var(--color-grey-70, #535966);
                            font-size: 13px;
                        `}
                    >
                        {popoverData.additionalInfo}
                    </p>
                )}

                {/* Version note */}
                {popoverData?.showVersionNote && popoverData?.versionNote && (
                    <p
                        css={css`
                            display: flex;
                            align-items: flex-start;
                            gap: 8px;
                            margin: 12px 0 0 0;
                            padding: 8px 12px;
                            background: var(--color-grey-5, #f5f7fa);
                            border-radius: 4px;
                            font-size: 12px;
                            color: var(--color-grey-70, #535966);
                            line-height: 1.5;
                        `}
                    >
                        <span
                            css={css`
                                flex-shrink: 0;
                                color: var(--color-blue-elastic, #0077cc);
                                font-size: 14px;
                            `}
                        >
                            ⓘ
                        </span>
                        {popoverData.versionNote}
                    </p>
                )}
            </div>
        </EuiPopover>
    )
}

customElements.define(
    'applies-to-popover',
    r2wc(AppliesToPopover, {
        props: {
            badgeKey: 'string',
            badgeLifecycleText: 'string',
            badgeVersion: 'string',
            lifecycleClass: 'string',
            lifecycleName: 'string',
            showLifecycleName: 'boolean',
            showVersion: 'boolean',
            hasMultipleLifecycles: 'boolean',
            popoverData: 'json',
            showPopover: 'boolean',
            isInline: 'boolean',
        },
    })
)

export default AppliesToPopover
