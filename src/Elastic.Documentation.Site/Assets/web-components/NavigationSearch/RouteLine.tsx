import type { ApiRoute } from './apiPropertyResults'
import { EuiHighlight, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'

/**
 * Splits a path like the search breadcrumb: the first segment always shows,
 * the middle truncates first, and the last literal segment (with any
 * parameters after it) is what tells routes apart, so it truncates last.
 */
export const splitRoutePath = (
    path: string
): { first: string; middle: string; last: string } => {
    const segments = path.split('/').slice(1)
    const lastLiteral = segments
        .map((segment) => !segment.startsWith('{'))
        .lastIndexOf(true)
    if (lastLiteral < 0) return { first: '', middle: '', last: path }
    const [first, ...middle] = segments.slice(0, lastLiteral)
    return {
        first: first === undefined ? '' : '/' + first,
        middle: middle.length ? '/' + middle.join('/') : '',
        last: '/' + segments.slice(lastLiteral).join('/'),
    }
}

/** The route a query named: the method the page leads with and the path. */
export const RouteLine = ({
    route: {
        path,
        methods: [method],
    },
    tokens,
}: {
    route: ApiRoute
    tokens: string[]
}) => {
    const { euiTheme } = useEuiTheme()
    const parts = Object.entries(splitRoutePath(path))
    return (
        <div
            className="route-line"
            css={css`
                display: flex;
                align-items: center;
                gap: ${euiTheme.size.s};
                min-width: 0;
                margin-top: ${euiTheme.size.xs};
                font-size: ${euiTheme.size.m};
                line-height: ${euiTheme.size.base};
                color: ${euiTheme.colors.textSubdued};
                .api-method {
                    height: calc(${euiTheme.size.base} + ${euiTheme.size.xxs});
                    padding: 0 ${euiTheme.size.xs};
                    border-radius: ${euiTheme.size.xs};
                    font-size: 10px;
                }
                code {
                    display: flex;
                    min-width: 0;
                    font-size: inherit;
                    color: ${euiTheme.colors.textParagraph};
                }
                code > span {
                    overflow: hidden;
                    white-space: nowrap;
                    text-overflow: ellipsis;
                }
                .route-first {
                    flex-shrink: 0;
                }
                .route-middle {
                    /* far above the last segment's default of 1, so the middle gives up its width first */
                    flex-shrink: 1000;
                }
                mark {
                    background-color: var(--search-highlight-tint);
                    color: inherit;
                    font-weight: ${euiTheme.font.weight.bold};
                    padding: 0;
                }
            `}
        >
            <span className={`api-method api-method-${method.toLowerCase()}`}>
                <span className="api-method-label">{method}</span>
            </span>
            <code title={path}>
                {parts.map(
                    ([name, text]) =>
                        text && (
                            <span key={name} className={`route-${name}`}>
                                <EuiHighlight
                                    search={tokens}
                                    highlightAll
                                    hasScreenReaderHelpText={false}
                                >
                                    {text}
                                </EuiHighlight>
                            </span>
                        )
                )}
            </code>
        </div>
    )
}
