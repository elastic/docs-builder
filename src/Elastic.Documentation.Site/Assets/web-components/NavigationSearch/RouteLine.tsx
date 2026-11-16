import type { ApiRoute } from './apiPropertyResults'
import { EuiHighlight, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'

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
                    overflow: hidden;
                    white-space: nowrap;
                    text-overflow: ellipsis;
                    font-size: inherit;
                    color: ${euiTheme.colors.textParagraph};
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
                <EuiHighlight
                    search={tokens}
                    highlightAll
                    hasScreenReaderHelpText={false}
                >
                    {path}
                </EuiHighlight>
            </code>
        </div>
    )
}
