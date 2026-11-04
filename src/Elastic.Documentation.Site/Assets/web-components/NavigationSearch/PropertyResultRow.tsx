import { useHtmxLink } from '../shared/htmx/useHtmxLink'
import { JumpToIndicator } from './JumpToIndicator'
import {
    GO_TO_LABELS,
    childLabel,
    fieldSearchTerms,
    type ApiPropertyResult,
} from './apiPropertyResults'
import {
    EuiHighlight,
    useEuiTheme,
    useIsWithinMaxBreakpoint,
} from '@elastic/eui'
import { css } from '@emotion/react'

/** Braces drawn as strokes; EUI's tokenObject is a filled token glyph that blurs at this size. */
const FieldIcon = () => (
    <svg
        width="16"
        height="16"
        viewBox="0 0 16 16"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
    >
        <path d="M6 2.5C4.6 2.5 4.2 3.2 4.2 4.4v1.2c0 1-.5 1.9-1.7 2.4 1.2.5 1.7 1.4 1.7 2.4v1.2c0 1.2.4 1.9 1.8 1.9M10 2.5c1.4 0 1.8.7 1.8 1.9v1.2c0 1 .5 1.9 1.7 2.4-1.2.5-1.7 1.4-1.7 2.4v1.2c0 1.2-.4 1.9-1.8 1.9" />
    </svg>
)

/** The page's tooltips show after 80ms; a mouse crossing result rows would open one on every row. */
const TOOLTIP_DELAY = '[500, 0]'

const MAX_VISIBLE_CHILDREN = 12

export const PropertyResultRow = ({
    property,
    searchTerm,
    showLocation,
    index,
    isSelected,
    isKeyboardNavigating,
    onMouseEnter,
    onMouseMove,
    onClick,
}: {
    property: ApiPropertyResult
    searchTerm: string
    showLocation: boolean
    index: number
    isSelected: boolean
    isKeyboardNavigating: boolean
    onMouseEnter: () => void
    onMouseMove: () => void
    onClick: () => void
}) => {
    const { euiTheme } = useEuiTheme()
    const isMobile = useIsWithinMaxBreakpoint('s')
    const { ref, href } = useHtmxLink(property.url)
    const leafOffset = property.path.lastIndexOf('.') + 1
    const propertySearch = fieldSearchTerms(searchTerm)
    const childLabels = property.children.map(childLabel)
    const fields =
        childLabels.length > 0 ? `Fields: ${childLabels.join(', ')}` : ''
    const hiddenChildren = property.children.length - MAX_VISIBLE_CHILDREN
    const highlight = (text: string) => (
        <EuiHighlight
            search={propertySearch}
            highlightAll
            hasScreenReaderHelpText={false}
        >
            {text}
        </EuiHighlight>
    )
    return (
        <a
            ref={ref}
            href={href}
            aria-label={`${property.path} ${property.type} ${property.location} ${property.description}${fields && ` ${fields}`}`}
            onClick={onClick}
            onMouseEnter={onMouseEnter}
            onMouseMove={onMouseMove}
            data-search-result-index={index}
            data-selected={isSelected ? 'true' : 'false'}
            data-keyboard-navigating={isKeyboardNavigating ? 'true' : 'false'}
            css={css`
                display: flex;
                align-items: center;
                gap: ${euiTheme.size.s};
                box-sizing: border-box;
                min-height: ${euiTheme.size.xl};
                padding: ${euiTheme.size.s} ${euiTheme.size.base};
                /* the Enter badge lines up with the operation row's, which insets it by size.l */
                padding-right: ${euiTheme.size.l};
                border-bottom: 1px solid ${euiTheme.colors.borderBaseSubdued};
                &:last-child {
                    border-bottom: none;
                }
                color: ${euiTheme.colors.textParagraph};
                font-size: ${euiTheme.size.m};
                line-height: calc(${euiTheme.size.base} + ${euiTheme.size.xs});
                text-decoration: none;
                background: ${isSelected ? euiTheme.colors.backgroundBaseSubdued : 'transparent'};
                mark {
                    background-color: var(--search-highlight-tint);
                    color: ${euiTheme.colors.textParagraph};
                    font-weight: ${euiTheme.font.weight.bold};
                    font-size: inherit;
                    padding: 0;
                }
                .property-icon {
                    display: grid;
                    flex: none;
                    place-items: center;
                    align-self: flex-start;
                    width: ${euiTheme.size.l};
                    height: ${euiTheme.size.l};
                    margin-top: 1px;
                    color: ${isSelected ? euiTheme.colors.primary : euiTheme.colors.textSubdued};
                }
                .property-body {
                    display: flex;
                    flex: 1;
                    flex-direction: column;
                    gap: ${euiTheme.size.xxs};
                    min-width: 0;
                }
                .property-path-line,
                .property-detail-line {
                    display: flex;
                    align-items: center;
                    gap: ${euiTheme.size.s};
                    min-width: 0;
                }
                .property-path,
                .property-type,
                .property-description {
                    min-width: 0;
                    overflow: hidden;
                    white-space: nowrap;
                    text-overflow: ellipsis;
                }
                .property-path {
                    flex: 0 1 auto;
                    /* rtl moves the ellipsis to the start so the leaf stays visible; the bdi keeps the path itself left to right */
                    direction: rtl;
                    text-align: left;
                    font-size: inherit;
                    line-height: inherit;
                    color: ${euiTheme.colors.textSubdued};
                }
                .property-path strong {
                    color: ${euiTheme.colors.textParagraph};
                    font-weight: ${euiTheme.font.weight.medium};
                }
                .property-location {
                    flex: none;
                    padding: 0 ${euiTheme.size.xs};
                    border: 1px solid ${euiTheme.colors.borderBaseSubdued};
                    border-radius: ${euiTheme.border.radius.medium};
                    color: ${euiTheme.colors.textSubdued};
                    font-size: ${euiTheme.font.scale.xs * euiTheme.base}px;
                    line-height: ${euiTheme.size.base};
                }
                .property-type {
                    flex: none;
                    max-width: 40%;
                    padding: 0 ${euiTheme.size.xs};
                    border-radius: ${euiTheme.border.radius.small};
                    background: ${euiTheme.colors.backgroundBaseSubdued};
                    color: ${euiTheme.colors.textSubdued};
                    font-family: ${euiTheme.font.familyCode};
                    font-size: ${euiTheme.font.scale.xs * euiTheme.base}px;
                    line-height: inherit;
                }
                .property-description {
                    flex: 1;
                    color: ${euiTheme.colors.textSubdued};
                }
                .property-children {
                    display: flex;
                    flex-wrap: wrap;
                    gap: ${euiTheme.size.xs} ${euiTheme.size.s};
                    margin-top: ${euiTheme.size.xxs};
                }
                .property-child {
                    padding: 0 ${euiTheme.size.xs};
                    border-radius: ${euiTheme.border.radius.small};
                    background: ${euiTheme.colors.backgroundBaseSubdued};
                    color: ${euiTheme.colors.textSubdued};
                    font-family: ${euiTheme.font.familyCode};
                    font-size: ${euiTheme.font.scale.xs * euiTheme.base}px;
                }
                .property-children-more {
                    color: ${euiTheme.colors.textSubdued};
                }
                .jump-to-indicator {
                    margin-left: ${euiTheme.size.s};
                }
                &:hover:not([data-keyboard-navigating='true']) {
                    background: ${euiTheme.colors.backgroundBaseHighlighted};

                    .property-path,
                    .property-path strong {
                        color: ${euiTheme.colors.link};
                    }

                    .property-path {
                        text-decoration: underline;
                    }
                }
                &[data-selected='true'] {
                    .property-path,
                    .property-path strong {
                        color: ${euiTheme.colors.link};
                    }

                    .property-path {
                        text-decoration: underline;
                    }
                }
                &:focus-visible {
                    outline: 2px solid ${euiTheme.colors.primary};
                    outline-offset: -2px;
                }
            `}
        >
            <span className="property-icon" aria-hidden="true">
                <FieldIcon />
            </span>
            <span className="property-body">
                <span className="property-path-line">
                    <code
                        className="property-path"
                        data-tippy-content={property.path}
                        data-tippy-delay={TOOLTIP_DELAY}
                        data-tippy-theme="mono"
                    >
                        <bdi>
                            {highlight(property.path.slice(0, leafOffset))}
                            <strong>
                                {highlight(property.path.slice(leafOffset))}
                            </strong>
                        </bdi>
                    </code>
                    <span
                        className={
                            showLocation ? 'property-location' : 'sr-only'
                        }
                    >
                        {property.location}
                    </span>
                </span>
                <span className="property-detail-line">
                    <code
                        className="property-type"
                        data-tippy-content={property.type}
                        data-tippy-delay={TOOLTIP_DELAY}
                        data-tippy-theme="mono"
                    >
                        {property.type}
                    </code>
                    <span
                        className="property-description"
                        data-tippy-content={property.description || undefined}
                        data-tippy-delay={TOOLTIP_DELAY}
                    >
                        {property.description}
                    </span>
                </span>
                {property.children.length > 0 && (
                    <span
                        className="property-children"
                        role="group"
                        aria-label={`Fields of ${property.path}`}
                    >
                        {property.children
                            .slice(0, MAX_VISIBLE_CHILDREN)
                            .map((suffix) => (
                                <span
                                    key={suffix}
                                    className="property-child"
                                    data-tippy-content={property.path + suffix}
                                    data-tippy-theme="mono"
                                    data-tippy-delay={TOOLTIP_DELAY}
                                >
                                    {childLabel(suffix)}
                                </span>
                            ))}
                        {hiddenChildren > 0 && (
                            <span
                                className="property-children-more"
                                data-tippy-content={childLabels.join(', ')}
                                data-tippy-theme="mono"
                                data-tippy-delay={TOOLTIP_DELAY}
                            >
                                +{hiddenChildren} more
                            </span>
                        )}
                    </span>
                )}
            </span>
            {!isMobile && <JumpToIndicator label={GO_TO_LABELS.property} />}
        </a>
    )
}
