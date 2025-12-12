'use strict'

import '../eui-icons-cache'
import { EuiPopover, useGeneratedHtmlId } from '@elastic/eui'
import { css } from '@emotion/react'
import r2wc from '@r2wc/react-to-web-component'
import * as React from 'react'
import { useState, useRef, useEffect, useCallback } from 'react'

type AppliesToPopoverProps = {
    badgeKey?: string
    badgeLifecycleText?: string
    badgeVersion?: string
    lifecycleClass?: string
    lifecycleName?: string
    showLifecycleName?: boolean
    showVersion?: boolean
    hasMultipleLifecycles?: boolean
    popoverContent?: string
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
    popoverContent,
    showPopover = true,
    isInline = false,
}: AppliesToPopoverProps) => {
    const [isOpen, setIsOpen] = useState(false)
    const [isPinned, setIsPinned] = useState(false)
    const popoverId = useGeneratedHtmlId({ prefix: 'appliesToPopover' })
    const contentRef = useRef<HTMLDivElement>(null)
    const badgeRef = useRef<HTMLSpanElement>(null)
    const hoverTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

    const openPopover = useCallback(() => {
        if (showPopover && popoverContent) {
            setIsOpen(true)
        }
    }, [showPopover, popoverContent])

    const closePopover = useCallback(() => {
        if (!isPinned) {
            setIsOpen(false)
        }
    }, [isPinned])

    const handleClick = useCallback(() => {
        if (showPopover && popoverContent) {
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
    }, [showPopover, popoverContent, isPinned])

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

    // Handle details/summary elements for collapsible sections
    useEffect(() => {
        if (!contentRef.current) return

        const details = contentRef.current.querySelectorAll('details')
        details.forEach((detail) => {
            detail.addEventListener('toggle', (e) => {
                e.stopPropagation()
            })
        })
    }, [isOpen, popoverContent])

    const showSeparator =
        badgeKey && (showLifecycleName || showVersion || badgeLifecycleText)

    const badgeButton = (
        <span
            ref={badgeRef}
            className={`applicable-info${showPopover && popoverContent ? ' applicable-info--clickable' : ''}${isPinned ? ' applicable-info--pinned' : ''}`}
            onClick={handleClick}
            onMouseEnter={handleMouseEnter}
            onMouseLeave={handleMouseLeave}
            role={showPopover && popoverContent ? 'button' : undefined}
            tabIndex={showPopover && popoverContent ? 0 : undefined}
            onKeyDown={(e) => {
                if (
                    showPopover &&
                    popoverContent &&
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

    if (!showPopover || !popoverContent) {
        return badgeButton
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
                    z-index: 40 !important; /* Below header which is z-50 */

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
                className="applies-to-popover-content"
                css={css`
                    padding: 16px;
                    font-size: 14px;
                    line-height: 1.5;
                    color: var(--color-grey-100, #1a1c21);

                    /* Product description */
                    .popover-product-description {
                        margin: 0 0 16px 0;
                        color: var(--color-grey-80, #343741);

                        strong {
                            display: inline;
                            font-weight: 700;
                            color: var(--color-grey-100, #1a1c21);
                        }
                    }

                    /* Availability section */
                    .popover-availability-title {
                        margin: 0 0 4px 0;

                        strong {
                            display: inline;
                            font-weight: 700;
                            font-size: 15px;
                            color: var(--color-grey-100, #1a1c21);
                        }
                    }

                    .popover-availability-intro {
                        margin: 0 0 8px 0;
                        color: var(--color-grey-70, #535966);
                        font-size: 13px;
                    }

                    /* Availability items (collapsible details/summary) */
                    .popover-availability-item {
                        margin: 0 0 4px 0;
                        border: none;
                        background: none;

                        &[open] .popover-availability-summary::before {
                            transform: rotate(45deg);
                        }
                    }

                    .popover-availability-summary {
                        display: flex;
                        align-items: center;
                        cursor: pointer;
                        list-style: none;
                        padding: 4px 0;
                        color: var(--color-blue-elastic, #0077cc);
                        font-weight: 500;

                        &::-webkit-details-marker {
                            display: none;
                        }

                        &::before {
                            content: '';
                            display: inline-block;
                            width: 6px;
                            height: 6px;
                            border-right: 2px solid currentColor;
                            border-bottom: 2px solid currentColor;
                            transform: rotate(-45deg);
                            margin-right: 8px;
                            transition: transform 0.15s ease;
                        }

                        &:hover {
                            color: var(--color-blue-hover, #005fa3);
                        }
                    }

                    .popover-availability-text {
                        flex: 1;
                    }

                    .popover-lifecycle-description {
                        margin: 4px 0 8px 16px;
                        padding: 8px 12px;
                        background: var(--color-grey-5, #f5f7fa);
                        border-radius: 4px;
                        font-size: 13px;
                        color: var(--color-grey-80, #343741);
                        line-height: 1.5;
                    }

                    /* Simple availability item (no collapsible content) */
                    .popover-availability-item-simple {
                        margin: 0 0 4px 0;
                        padding: 4px 0 4px 16px;
                        color: var(--color-grey-80, #343741);
                    }

                    /* Additional availability info */
                    .popover-additional-info {
                        margin: 12px 0 0 0;
                        padding-top: 12px;
                        border-top: 1px solid var(--color-grey-15, #e0e5ee);
                        color: var(--color-grey-70, #535966);
                        font-size: 13px;
                    }

                    /* Version note */
                    .popover-version-note {
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
                    }

                    .popover-note-icon {
                        flex-shrink: 0;
                        color: var(--color-blue-elastic, #0077cc);
                        font-size: 14px;
                    }
                `}
                dangerouslySetInnerHTML={{ __html: popoverContent }}
            />
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
            popoverContent: 'string',
            showPopover: 'boolean',
            isInline: 'boolean',
        },
    })
)

export default AppliesToPopover
