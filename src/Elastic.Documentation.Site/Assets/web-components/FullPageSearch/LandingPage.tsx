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
import { useState, useEffect, useRef, ReactNode } from 'react'

// Inline SVG icons from src/Elastic.Markdown/Myst/Roles/Icons/svgs
const QuestionIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width="24"
        height="24"
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path d="M8 10a1 1 0 1 1 0 2 1 1 0 0 1 0-2Zm0-6a2 2 0 0 1 1.237 3.571 3.59 3.59 0 0 0-.548.505c-.137.169-.189.305-.189.424V9h-1v-.5c0-.433.195-.787.413-1.055.216-.265.487-.487.705-.659a1 1 0 1 0-1.562-.453l-.942.334A2 2 0 0 1 8 4Z" />
        <path d="M8 1a7 7 0 1 1 0 14A7 7 0 0 1 8 1Zm0 1a6 6 0 1 0 0 12A6 6 0 0 0 8 2Z" />
    </svg>
)

const SearchIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width="24"
        height="24"
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path d="M6.5 1a5.5 5.5 0 0 1 4.729 8.308l3.421 2.933a1 1 0 0 1 .057 1.466l-1 1a1 1 0 0 1-1.466-.057l-2.933-3.42A5.5 5.5 0 1 1 6.5 1Zm4.139 9.12a5.516 5.516 0 0 1-.52.519L13 14l1-1-3.361-2.88ZM6.5 2a4.5 4.5 0 1 0 .314 8.987c.024-.001.047-.004.07-.006.207-.017.41-.048.607-.092l.066-.016a4.41 4.41 0 0 0 .588-.185c.012-.006.026-.01.039-.015.194-.079.382-.171.562-.275l.03-.017a4.52 4.52 0 0 0 1.605-1.605c.006-.01.01-.02.017-.03.104-.18.196-.368.275-.562l.018-.048c.074-.188.134-.38.182-.58l.016-.065a4.49 4.49 0 0 0 .093-.61l.005-.067a4.544 4.544 0 0 0 .007-.545A4.5 4.5 0 0 0 6.5 2Z" />
    </svg>
)

const IngestIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width="24"
        height="24"
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path d="M1.747,10.9922 L1.747,10.992 L14.289,10.992 L14.289,10.9922 L14.8471,10.9922 C14.9141,10.9922 14.9691,10.9362 14.9691,10.8692 L14.9691,8.5102 C14.9691,8.4422 14.9141,8.3882 14.8471,8.3882 L1.1221,8.3882 C1.0551,8.3882 1.0001,8.4422 1.0001,8.5102 L1.0001,10.8692 C1.0001,10.9362 1.0551,10.9922 1.1221,10.9922 L1.747,10.9922 Z M13.7581,11.992 L2.2101,11.992 L2.2101,16.0002 L1.2101,16.0002 L1.2101,11.9922 L1.1221,11.9922 C0.5031,11.9922 0.0001,11.4882 0.0001,10.8692 L0.0001,8.5102 C0.0001,7.8902 0.5031,7.3882 1.1221,7.3882 L14.8471,7.3882 C15.4661,7.3882 15.9691,7.8902 15.9691,8.5102 L15.9691,10.8692 C15.9691,11.4882 15.4661,11.9922 14.8471,11.9922 L14.7581,11.9922 L14.7581,16.0002 L13.7581,16.0002 L13.7581,11.992 Z M7.488,4.50534991 L7.488,-0.001 L8.488,-0.001 L8.488,4.52789914 L10.895,2.2664 L11.58,2.9954 L8,6.3574 L4.42,2.9954 L5.105,2.2664 L7.488,4.50534991 Z" />
    </svg>
)

const SparklesIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width="24"
        height="24"
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path
            fillRule="evenodd"
            d="M12 .5a.5.5 0 0 0-1 0c0 .42-.13 1.061-.506 1.583C10.137 2.579 9.537 3 8.5 3a.5.5 0 0 0 0 1c1.037 0 1.637.42 1.994.917C10.87 5.44 11 6.08 11 6.5a.5.5 0 0 0 1 0c0-.42.13-1.061.506-1.583.357-.496.957-.917 1.994-.917a.5.5 0 0 0 0-1c-1.037 0-1.637-.42-1.994-.917A2.852 2.852 0 0 1 12 .5Zm.584 3a3.1 3.1 0 0 1-.89-.833 3.407 3.407 0 0 1-.194-.302 3.407 3.407 0 0 1-.194.302 3.1 3.1 0 0 1-.89.833 3.1 3.1 0 0 1 .89.833c.07.099.136.2.194.302.059-.102.123-.203.194-.302a3.1 3.1 0 0 1 .89-.833ZM6 3.5a.5.5 0 0 0-1 0v.006a1.984 1.984 0 0 1-.008.173 5.64 5.64 0 0 1-.063.52 5.645 5.645 0 0 1-.501 1.577c-.283.566-.7 1.117-1.315 1.527C2.501 7.71 1.663 8 .5 8a.5.5 0 0 0 0 1c1.163 0 2.001.29 2.613.697.616.41 1.032.96 1.315 1.527.284.567.428 1.14.5 1.577a5.645 5.645 0 0 1 .072.693v.005a.5.5 0 0 0 1 .001v-.006a1.995 1.995 0 0 1 .008-.173 6.14 6.14 0 0 1 .063-.52c.073-.436.217-1.01.501-1.577.283-.566.7-1.117 1.315-1.527C8.499 9.29 9.337 9 10.5 9a.5.5 0 0 0 0-1c-1.163 0-2.001-.29-2.613-.697-.616-.41-1.032-.96-1.315-1.527a5.645 5.645 0 0 1-.5-1.577A5.64 5.64 0 0 1 6 3.506V3.5Zm1.989 5a4.717 4.717 0 0 1-.657-.365c-.791-.528-1.312-1.227-1.654-1.911a5.943 5.943 0 0 1-.178-.391c-.053.13-.112.26-.178.39-.342.685-.863 1.384-1.654 1.912a4.718 4.718 0 0 1-.657.365c.236.108.454.23.657.365.791.528 1.312 1.227 1.654 1.911.066.131.125.262.178.391.053-.13.112-.26.178-.39.342-.685.863-1.384 1.654-1.912.203-.135.421-.257.657-.365ZM12.5 9a.5.5 0 0 1 .5.5c0 .42.13 1.061.506 1.583.357.496.957.917 1.994.917a.5.5 0 0 1 0 1c-1.037 0-1.637.42-1.994.917A2.852 2.852 0 0 0 13 15.5a.5.5 0 0 1-1 0c0-.42-.13-1.061-.506-1.583-.357-.496-.957-.917-1.994-.917a.5.5 0 0 1 0-1c1.037 0 1.637-.42 1.994-.917A2.852 2.852 0 0 0 12 9.5a.5.5 0 0 1 .5-.5Zm.194 2.667c.23.32.524.607.89.833a3.1 3.1 0 0 0-.89.833 3.42 3.42 0 0 0-.194.302 3.42 3.42 0 0 0-.194-.302 3.1 3.1 0 0 0-.89-.833 3.1 3.1 0 0 0 .89-.833c.07-.099.136-.2.194-.302.059.102.123.203.194.302Z"
            clipRule="evenodd"
        />
    </svg>
)

interface RotatingQuery {
    template: string
    variants: string[]
    icon: ReactNode
}

const ROTATING_QUERIES: RotatingQuery[] = [
    {
        template: 'What is {variable}?',
        variants: ['Elasticsearch', 'Logstash', 'Kibana', 'Beats'],
        icon: <QuestionIcon />,
    },
    {
        template: 'How do I query with {variable}?',
        variants: ['Elasticsearch', 'ES|QL', 'Kibana', 'KQL'],
        icon: <SearchIcon />,
    },
    {
        template: 'How do I ingest data with {variable}?',
        variants: ['Logstash', 'Beats', 'Fleet', 'Elastic Agent'],
        icon: <IngestIcon />,
    },
    {
        template: 'How does {variable} work?',
        variants: [
            'machine learning',
            'anomaly detection',
            'inference',
            'vector search',
        ],
        icon: <SparklesIcon />,
    },
]

const POPULAR_SEARCHES = [
    'getting started',
    'ingest pipelines',
    'aggregations',
    'mapping',
    'security',
]

function useRotatingIndex(length: number, baseInterval = 2500, variance = 300) {
    const [index, setIndex] = useState(0)
    const [prevIndex, setPrevIndex] = useState<number | null>(null)
    const [isAnimating, setIsAnimating] = useState(false)

    useEffect(() => {
        let timer: ReturnType<typeof setTimeout>

        const scheduleNext = () => {
            // Add randomness to create drift between cards
            const randomOffset = (Math.random() - 0.5) * 2 * variance
            const interval = baseInterval + randomOffset
            timer = setTimeout(() => {
                setIndex((prev) => {
                    setPrevIndex(prev)
                    setIsAnimating(true)
                    return (prev + 1) % length
                })
                // Reset animation state after animation completes
                setTimeout(() => setIsAnimating(false), 500)
                scheduleNext()
            }, interval)
        }

        scheduleNext()

        return () => clearTimeout(timer)
    }, [length, baseInterval, variance])

    return { index, prevIndex, isAnimating }
}

interface RotatingQueryCardProps {
    template: string
    variants: string[]
    icon: ReactNode
    onSelect: (query: string) => void
}

const RotatingQueryCard = ({
    template,
    variants,
    icon,
    onSelect,
}: RotatingQueryCardProps) => {
    const { euiTheme } = useEuiTheme()
    const { index, prevIndex, isAnimating } = useRotatingIndex(variants.length)
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
                height: 100px;

                &:hover {
                    border-color: ${euiTheme.colors.primary};
                    box-shadow: ${euiTheme.levels.menu};
                }
            `}
        >
            <EuiFlexGroup alignItems="center" gutterSize="m" css={css`height: 100%;`}>
                <EuiFlexItem grow={false}>
                    <div
                        css={css`
                            padding: ${euiTheme.size.m};
                            background: ${euiTheme.colors.lightestShade};
                            border-radius: ${euiTheme.border.radius.medium};
                            color: ${euiTheme.colors.primary};
                            display: flex;
                            align-items: center;
                            justify-content: center;
                        `}
                    >
                        {icon}
                    </div>
                </EuiFlexItem>
                <EuiFlexItem>
                    <EuiText size="m">
                        <span>{parts[0]}<span
                            css={css`
                                display: inline-grid;
                                vertical-align: baseline;
                                text-align: center;
                                background: ${euiTheme.colors.lightestShade};
                                border-radius: ${euiTheme.border.radius.small};
                                padding: 0 ${euiTheme.size.xs};
                                perspective: 200px;
                                overflow: hidden;
                            `}
                        >{variants.map((variant, i) => {
                            const isCurrent = i === index
                            const isPrev = i === prevIndex

                            // Slot machine style: old text rolls up and out, new text rolls up from below
                            let transform = 'translateY(100%) rotateX(-90deg)'
                            let opacity = 0

                            if (isCurrent) {
                                // Current item is visible and in place
                                transform = 'translateY(0) rotateX(0deg)'
                                opacity = 1
                            } else if (isPrev && isAnimating) {
                                // Previous item rolls up and fades out
                                transform = 'translateY(-100%) rotateX(90deg)'
                                opacity = 0
                            }

                            return (
                                <span
                                    key={variant}
                                    css={css`
                                        grid-area: 1 / 1;
                                        font-weight: ${euiTheme.font.weight.semiBold};
                                        color: ${euiTheme.colors.primary};
                                        transform-style: preserve-3d;
                                        backface-visibility: hidden;
                                        transition: transform 0.5s cubic-bezier(0.34, 1.56, 0.64, 1),
                                                    opacity 0.3s ease-out;
                                        transform: ${transform};
                                        opacity: ${opacity};
                                        transform-origin: center bottom;
                                    `}
                                >
                                    {variant}
                                </span>
                            )
                        })}</span>{parts[1]}</span>
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
                width: 100%;
                max-width: 1200px;
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
                    grid-template-columns: 1fr;
                    gap: ${euiTheme.size.m};
                    margin-top: ${euiTheme.size.xl};

                    @media (min-width: 768px) {
                        grid-template-columns: repeat(2, 1fr);
                    }
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
