import { config } from '../../config'
import { logInfo } from '../../telemetry/logging'
import {
    EuiFlexGroup,
    EuiFlexItem,
    EuiIcon,
    EuiLoadingSpinner,
    EuiPanel,
    EuiText,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useEffect, useState } from 'react'

export interface RelatedPage {
    url: string
    title: string
    description: string
    parents: Array<{ title: string; url: string }>
}

interface RelatedPagesResponse {
    query: string
    results: RelatedPage[]
}

export const RelatedPages = ({
    path = window.location.pathname,
}: {
    path?: string
}) => {
    const { euiTheme } = useEuiTheme()
    const [results, setResults] = useState<RelatedPage[]>([])
    const [isLoading, setIsLoading] = useState(true)

    useEffect(() => {
        const controller = new AbortController()

        const load = async () => {
            try {
                const response = await fetch(
                    `${config.apiBasePath}/v1/related-pages?path=${encodeURIComponent(path)}`,
                    { signal: controller.signal }
                )
                if (!response.ok) return

                const data = (await response.json()) as RelatedPagesResponse
                setResults(data.results)
                logInfo('404 related pages loaded', {
                    '404.related_pages.results_count': data.results.length,
                })
            } catch (error) {
                if (error instanceof Error && error.name === 'AbortError')
                    return
            } finally {
                if (!controller.signal.aborted) setIsLoading(false)
            }
        }

        if (config.airGapped) {
            setIsLoading(false)
            return () => controller.abort()
        }

        void load()
        return () => controller.abort()
    }, [path])

    if (isLoading) {
        return (
            <div
                role="status"
                css={css`
                    display: flex;
                    justify-content: center;
                    padding: ${euiTheme.size.l};
                `}
            >
                <EuiLoadingSpinner size="l" />
                <span className="sr-only">Finding related pages</span>
            </div>
        )
    }

    if (results.length === 0) return null

    return (
        <section aria-labelledby="related-pages-heading">
            <EuiText>
                <h2 id="related-pages-heading">You might be looking for</h2>
            </EuiText>
            <EuiFlexGroup
                direction="column"
                gutterSize="m"
                css={css`
                    margin-top: ${euiTheme.size.base};
                `}
            >
                {results.map((result, index) => (
                    <EuiFlexItem key={result.url}>
                        <EuiPanel hasBorder paddingSize="m">
                            <a
                                href={result.url}
                                onClick={() =>
                                    logInfo('404 related page clicked', {
                                        '404.related_pages.result_url':
                                            result.url,
                                        '404.related_pages.result_position':
                                            index,
                                    })
                                }
                                css={css`
                                    display: block;
                                    color: inherit;
                                    text-decoration: none;

                                    &:hover h3 {
                                        color: ${euiTheme.colors.primary};
                                        text-decoration: underline;
                                    }
                                `}
                            >
                                {result.parents.length > 0 && (
                                    <div
                                        css={css`
                                            margin-bottom: ${euiTheme.size.xs};
                                            color: ${euiTheme.colors
                                                .subduedText};
                                            font-size: ${euiTheme.font.scale
                                                .xs * euiTheme.base}px;
                                        `}
                                    >
                                        {result.parents
                                            .map((parent) => parent.title)
                                            .join(' › ')}
                                    </div>
                                )}
                                <EuiFlexGroup
                                    alignItems="center"
                                    gutterSize="s"
                                    responsive={false}
                                >
                                    <EuiFlexItem grow={false}>
                                        <EuiIcon type="document" />
                                    </EuiFlexItem>
                                    <EuiFlexItem>
                                        <EuiText>
                                            <h3>{result.title}</h3>
                                        </EuiText>
                                    </EuiFlexItem>
                                </EuiFlexGroup>
                                {result.description && (
                                    <EuiText size="s" color="subdued">
                                        <p
                                            css={css`
                                                margin-top: ${euiTheme.size.xs};
                                            `}
                                        >
                                            {result.description}
                                        </p>
                                    </EuiText>
                                )}
                            </a>
                        </EuiPanel>
                    </EuiFlexItem>
                ))}
            </EuiFlexGroup>
        </section>
    )
}
