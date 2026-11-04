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
            title={`${property.location}: ${property.path}\n${property.type}\n${property.description}${fields && `\n${fields}`}`}
            onClick={onClick}
            onMouseEnter={onMouseEnter}
            onMouseMove={onMouseMove}
            data-search-result-index={index}
            data-selected={isSelected ? 'true' : 'false'}
            data-keyboard-navigating={isKeyboardNavigating ? 'true' : 'false'}
            css={css`
                display: flex;
                position: relative;
                align-items: center;
                gap: ${euiTheme.size.s};
                box-sizing: border-box;
                min-height: ${euiTheme.size.xl};
                padding: ${euiTheme.size.xs} ${euiTheme.size.base};
                padding-left: calc(${euiTheme.size.xl} + ${euiTheme.size.base});
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
                &::before {
                    content: '';
                    position: absolute;
                    left: ${euiTheme.size.xl};
                    top: 0;
                    bottom: 0;
                    border-left: 1px solid ${euiTheme.colors.borderBaseSubdued};
                }
                mark {
                    background-color: var(--search-highlight-tint);
                    color: ${euiTheme.colors.textParagraph};
                    font-weight: ${euiTheme.font.weight.bold};
                    font-size: inherit;
                    padding: 0;
                }
                .property-body {
                    display: flex;
                    flex: 1;
                    flex-direction: column;
                    min-width: 0;
                }
                .property-line {
                    display: flex;
                    align-items: center;
                    gap: ${euiTheme.size.s};
                    min-width: 0;
                }
                .property-children {
                    overflow: hidden;
                    white-space: nowrap;
                    text-overflow: ellipsis;
                    /* a monospace space is 1ch wide, so each gap comes out at size.m while the line still ellipsizes as text */
                    word-spacing: calc(${euiTheme.size.m} - 1ch);
                    color: ${euiTheme.colors.textSubdued};
                    font-family: ${euiTheme.font.familyCode};
                }
                .property-line > code,
                .property-description,
                .property-path > span {
                    min-width: 0;
                    overflow: hidden;
                    white-space: nowrap;
                    text-overflow: ellipsis;
                    color: ${euiTheme.colors.textSubdued};
                }
                .property-line > code {
                    flex: 0 1 auto;
                    max-width: 28%;
                    font-size: inherit;
                    line-height: inherit;
                }
                .property-line > .property-path {
                    display: flex;
                    max-width: 55%;
                    color: inherit;
                }
                .property-path > strong {
                    flex-shrink: 0;
                    font-weight: ${euiTheme.font.weight.medium};
                }
                .property-location {
                    order: 1;
                    flex-shrink: 0;
                    color: ${euiTheme.colors.textSubdued};
                }
                .property-description {
                    flex: 1;
                }
                .jump-to-indicator {
                    order: 2;
                    margin-left: ${euiTheme.size.s};
                }
                &:hover:not([data-keyboard-navigating='true']) {
                    background: ${euiTheme.colors.backgroundBaseHighlighted};
                }
                &:focus-visible {
                    outline: 2px solid ${euiTheme.colors.primary};
                    outline-offset: -2px;
                }
            `}
        >
            <span className="property-body">
                <span className="property-line">
                    <code className="property-path" title={property.path}>
                        {highlight(property.path.slice(0, leafOffset))}
                        <strong>
                            {highlight(property.path.slice(leafOffset))}
                        </strong>
                    </code>
                    <code title={property.type}>{property.type}</code>
                    <span
                        className={
                            showLocation ? 'property-location' : 'sr-only'
                        }
                    >
                        {property.location}
                    </span>
                    <span className="property-description">
                        {property.description}
                    </span>
                </span>
                {childLabels.length > 0 && (
                    <span className="property-children">
                        {childLabels.join(' ')}
                    </span>
                )}
            </span>
            {!isMobile && <JumpToIndicator label={GO_TO_LABELS.property} />}
        </a>
    )
}
