import {
    EuiFlexGroup,
    EuiFlexItem,
    EuiText,
    EuiTitle,
    EuiIcon,
    EuiBadge,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useState, useEffect, useRef } from 'react'

interface RotatingQuery {
    template: string
    variants: string[]
    icon: string
}

const ROTATING_QUERIES: RotatingQuery[] = [
    {
        template: 'What is {variable}?',
        variants: ['Elasticsearch', 'Logstash', 'Kibana', 'Beats'],
        icon: 'questionInCircle',
    },
    {
        template: 'How do I query with {variable}?',
        variants: ['Elasticsearch', 'ES|QL', 'Kibana', 'KQL'],
        icon: 'search',
    },
    {
        template: 'How do I ingest data with {variable}?',
        variants: ['Logstash', 'Beats', 'Fleet', 'Elastic Agent'],
        icon: 'database',
    },
    {
        template: 'What is {variable}?',
        variants: [
            'machine learning',
            'anomaly detection',
            'inference',
            'vector search',
        ],
        icon: 'sparkles',
    },
]

const POPULAR_SEARCHES = [
    'getting started',
    'ingest pipelines',
    'aggregations',
    'mapping',
    'security',
]

function useRotatingIndex(length: number, interval = 2500) {
    const [index, setIndex] = useState(0)

    useEffect(() => {
        const timer = setInterval(() => {
            setIndex((prev) => (prev + 1) % length)
        }, interval)
        return () => clearInterval(timer)
    }, [length, interval])

    return index
}

interface RotatingQueryCardProps {
    template: string
    variants: string[]
    icon: string
    onSelect: (query: string) => void
}

const RotatingQueryCard = ({
    template,
    variants,
    icon,
    onSelect,
}: RotatingQueryCardProps) => {
    const { euiTheme } = useEuiTheme()
    const index = useRotatingIndex(variants.length)
    const [isAnimating, setIsAnimating] = useState(false)
    const prevIndex = useRef(index)

    useEffect(() => {
        if (prevIndex.current !== index) {
            setIsAnimating(true)
            const timer = setTimeout(() => setIsAnimating(false), 300)
            prevIndex.current = index
            return () => clearTimeout(timer)
        }
    }, [index])

    const currentQuery = template.replace('{variable}', variants[index])
    const parts = template.split('{variable}')

    return (
        <button
            onClick={() => onSelect(currentQuery)}
            css={css`
                background: ${euiTheme.colors.emptyShade};
                border: 1px solid ${euiTheme.border.color};
                border-radius: ${euiTheme.border.radius.medium};
                padding: ${euiTheme.size.l};
                text-align: left;
                cursor: pointer;
                transition: all 0.2s ease;
                width: 100%;

                &:hover {
                    border-color: ${euiTheme.colors.primary};
                    box-shadow: ${euiTheme.levels.menu};
                }
            `}
        >
            <EuiFlexGroup alignItems="flexStart" gutterSize="m">
                <EuiFlexItem grow={false}>
                    <div
                        css={css`
                            padding: ${euiTheme.size.m};
                            background: ${euiTheme.colors.lightestShade};
                            border-radius: ${euiTheme.border.radius.medium};
                            color: ${euiTheme.colors.primary};
                        `}
                    >
                        <EuiIcon type={icon} size="l" />
                    </div>
                </EuiFlexItem>
                <EuiFlexItem>
                    <EuiText size="m">
                        <span>
                            {parts[0]}
                            <span
                                css={css`
                                    font-weight: ${euiTheme.font.weight.semiBold};
                                    color: ${euiTheme.colors.primary};
                                    display: inline-block;
                                    transition: all 0.3s ease;
                                    opacity: ${isAnimating ? 0 : 1};
                                    transform: translateY(
                                        ${isAnimating ? '8px' : '0'}
                                    );
                                `}
                            >
                                {variants[index]}
                            </span>
                            {parts[1]}
                        </span>
                    </EuiText>
                    <EuiFlexGroup
                        gutterSize="xs"
                        css={css`
                            margin-top: ${euiTheme.size.s};
                        `}
                    >
                        {variants.map((_, i) => (
                            <EuiFlexItem key={i} grow={false}>
                                <div
                                    css={css`
                                        width: 6px;
                                        height: 6px;
                                        border-radius: 50%;
                                        background: ${i === index
                                            ? euiTheme.colors.primary
                                            : euiTheme.colors.lightShade};
                                        transition: background 0.2s ease;
                                    `}
                                />
                            </EuiFlexItem>
                        ))}
                    </EuiFlexGroup>
                </EuiFlexItem>
                <EuiFlexItem grow={false}>
                    <EuiIcon type="arrowRight" color="subdued" />
                </EuiFlexItem>
            </EuiFlexGroup>
        </button>
    )
}

interface LandingPageProps {
    onSearch: (query: string) => void
}

export const LandingPage = ({ onSearch }: LandingPageProps) => {
    const { euiTheme } = useEuiTheme()

    return (
        <div
            css={css`
                max-width: 900px;
                margin: 0 auto;
                padding: ${euiTheme.size.xxl} ${euiTheme.size.l};
            `}
        >
            <EuiFlexGroup
                direction="column"
                alignItems="center"
                gutterSize="l"
            >
                <EuiFlexItem>
                    <EuiTitle size="l">
                        <h1
                            css={css`
                                text-align: center;
                            `}
                        >
                            Search Elastic Documentation
                        </h1>
                    </EuiTitle>
                </EuiFlexItem>
                <EuiFlexItem>
                    <EuiText
                        color="subdued"
                        textAlign="center"
                        css={css`
                            max-width: 600px;
                        `}
                    >
                        <p>
                            Find guides, API references, tutorials, and more.
                            Ask a question or search by keyword.
                        </p>
                    </EuiText>
                </EuiFlexItem>
            </EuiFlexGroup>

            <div
                css={css`
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
                    gap: ${euiTheme.size.m};
                    margin-top: ${euiTheme.size.xl};
                `}
            >
                {ROTATING_QUERIES.map((query, idx) => (
                    <RotatingQueryCard
                        key={idx}
                        template={query.template}
                        variants={query.variants}
                        icon={query.icon}
                        onSelect={onSearch}
                    />
                ))}
            </div>

            <EuiFlexGroup
                direction="column"
                alignItems="center"
                gutterSize="s"
                css={css`
                    margin-top: ${euiTheme.size.xxl};
                `}
            >
                <EuiFlexItem>
                    <EuiText size="s" color="subdued">
                        Popular searches
                    </EuiText>
                </EuiFlexItem>
                <EuiFlexItem>
                    <EuiFlexGroup gutterSize="s" wrap>
                        {POPULAR_SEARCHES.map((term) => (
                            <EuiFlexItem key={term} grow={false}>
                                <EuiBadge
                                    color="hollow"
                                    onClick={() => onSearch(term)}
                                    onClickAriaLabel={`Search for ${term}`}
                                    css={css`
                                        cursor: pointer;
                                        &:hover {
                                            background: ${euiTheme.colors
                                                .lightestShade};
                                        }
                                    `}
                                >
                                    {term}
                                </EuiBadge>
                            </EuiFlexItem>
                        ))}
                    </EuiFlexGroup>
                </EuiFlexItem>
            </EuiFlexGroup>
        </div>
    )
}
