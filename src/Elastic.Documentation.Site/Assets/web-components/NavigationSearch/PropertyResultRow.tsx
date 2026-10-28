import { useHtmxLink } from '../shared/htmx/useHtmxLink'
import type { ApiPropertyResult } from './apiPropertyResults'
import { EuiHighlight, useEuiTheme } from '@elastic/eui'
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
    const { ref, href } = useHtmxLink(property.url)
    const leafOffset = property.path.lastIndexOf('.') + 1
    const propertySearch =
        searchTerm.trim().split(/\s+/).pop()?.split('.').filter(Boolean) ?? []
    return (
        <a
            ref={ref}
            href={href}
            aria-label={`${property.path} ${property.type} ${property.location} ${property.description}`}
            title={`${property.location}: ${property.path}\n${property.type}\n${property.description}`}
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
                    background-color: color-mix(
                        in srgb,
                        var(--color-yellow-40) 40%,
                        transparent
                    );
                    color: ${euiTheme.colors.textParagraph};
                    font-weight: ${euiTheme.font.weight.bold};
                    font-size: inherit;
                    padding: 0;
                }
                > code,
                .property-description,
                .property-path > span {
                    min-width: 0;
                    overflow: hidden;
                    white-space: nowrap;
                    text-overflow: ellipsis;
                    color: ${euiTheme.colors.textSubdued};
                }
                > code {
                    flex: 0 1 auto;
                    max-width: 28%;
                    font-size: inherit;
                    line-height: inherit;
                }
                > .property-path {
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
                &:hover:not([data-keyboard-navigating='true']) {
                    background: ${euiTheme.colors.backgroundBaseHighlighted};
                }
                &:focus-visible {
                    outline: 2px solid ${euiTheme.colors.primary};
                    outline-offset: -2px;
                }
            `}
        >
            <code className="property-path" title={property.path}>
                <EuiHighlight
                    search={propertySearch}
                    highlightAll
                    hasScreenReaderHelpText={false}
                >
                    {property.path.slice(0, leafOffset)}
                </EuiHighlight>
                <strong>
                    <EuiHighlight
                        search={propertySearch}
                        highlightAll
                        hasScreenReaderHelpText={false}
                    >
                        {property.path.slice(leafOffset)}
                    </EuiHighlight>
                </strong>
            </code>
            <code title={property.type}>{property.type}</code>
            <span className={showLocation ? 'property-location' : 'sr-only'}>
                {property.location}
            </span>
            <span className="property-description">{property.description}</span>
        </a>
    )
}
