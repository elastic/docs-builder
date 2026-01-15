import type { FullPageSearchFilters } from './fullPageSearch.store'
import { getProductDisplayName } from './productsConfig'
import type { SearchAggregations } from './useFullPageSearchQuery'
import { EuiBadge, EuiIcon, EuiText, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'
import { ReactNode, useState } from 'react'

const ITEMS_TO_SHOW = 5

// Inline SVG icons
const PackageIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width="16"
        height="16"
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path d="M14.4472,3.72363l-6-3a.99965.99965,0,0,0-.8944,0l-6,3A.99992.99992,0,0,0,1,4.618v6.764a1.00012,1.00012,0,0,0,.5528.89441l6,3a1,1,0,0,0,.8944,0l6-3A1.00012,1.00012,0,0,0,15,11.382V4.618A.99992.99992,0,0,0,14.4472,3.72363ZM5.87085,5.897l5.34357-2.67181L13.372,4.304,8,6.94293ZM8,1.618,10.09637,2.6662,4.74292,5.343,2.628,4.30408ZM2,5.10968,4.25,6.215V9a.5.5,0,0,0,1,0V6.70618L7.5,7.81146V14.132L2,11.382ZM8.5,14.132V7.81146L14,5.10968V11.382Z" />
    </svg>
)

const DocumentIcon = ({ size = 16 }: { size?: number }) => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width={size}
        height={size}
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path
            fillRule="evenodd"
            d="M3 2a1 1 0 0 1 1-1h4.707L13 5.293V14a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V2Zm5 0H4v12h8V6H9a1 1 0 0 1-1-1V2Zm1 .707L11.293 5H9V2.707Z"
            clipRule="evenodd"
        />
    </svg>
)

const ConsoleIcon = ({ size = 16 }: { size?: number }) => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width={size}
        height={size}
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path d="M1.157 12.224 5.768 8.32a.404.404 0 0 0 0-.64l-4.61-3.904a.407.407 0 0 1 0-.643.608.608 0 0 1 .759 0l4.61 3.904c.631.534.63 1.393 0 1.926l-4.61 3.904a.608.608 0 0 1-.76 0 .407.407 0 0 1 0-.643ZM9 12h6v1H9z" />
    </svg>
)

const LayersIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width="16"
        height="16"
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path
            fillRule="evenodd"
            d="M1.553 4.106a1 1 0 0 0 0 1.788l6 3a1 1 0 0 0 .894 0l6-3a1 1 0 0 0 0-1.788l-6-3a1 1 0 0 0-.894 0l-6 3ZM14 5 8 8 2 5l6-3 6 3Z"
            clipRule="evenodd"
        />
        <path d="m8 11 6.894-3.447S15 7.843 15 8a1 1 0 0 1-.553.895l-6 3a1 1 0 0 1-.894 0l-6-3A1 1 0 0 1 1 8c0-.158.106-.447.106-.447L8 11Z" />
        <path d="m8 14 6.894-3.447s.106.29.106.447a1 1 0 0 1-.553.895l-6 3a1 1 0 0 1-.894 0l-6-3A1 1 0 0 1 1 11c0-.158.106-.447.106-.447L8 14Z" />
    </svg>
)

const CloudIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width="16"
        height="16"
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path d="M10.7458178,5.00539515 C13.6693111,5.13397889 16,7.54480866 16,10.5 C16,13.5375661 13.5375661,16 10.5,16 L4,16 C1.790861,16 0,14.209139 0,12 C0,10.3632968 0.983004846,8.95618668 2.39082809,8.33685609 C2.20209747,7.91526722 2.07902056,7.46524082 2.02752078,7 L0.5,7 C0.223857625,7 0,6.77614237 0,6.5 C0,6.22385763 0.223857625,6 0.5,6 L2.02746439,6 C2.1234088,5.13207349 2.46618907,4.33854089 2.98405031,3.69115709 L1.64644661,2.35355339 C1.45118446,2.15829124 1.45118446,1.84170876 1.64644661,1.64644661 C1.84170876,1.45118446 2.15829124,1.45118446 2.35355339,1.64644661 L3.69115709,2.98405031 C4.33854089,2.46618907 5.13207349,2.1234088 6,2.02746439 L6,0.5 C6,0.223857625 6.22385763,8.8817842e-16 6.5,8.8817842e-16 C6.77614237,8.8817842e-16 7,0.223857625 7,0.5 L7,2.02763291 C7.86199316,2.12340881 8.65776738,2.46398198 9.30889273,2.98400049 L10.6464466,1.64644661 C10.8417088,1.45118446 11.1582912,1.45118446 11.3535534,1.64644661 C11.5488155,1.84170876 11.5488155,2.15829124 11.3535534,2.35355339 L10.0163882,3.69071859 C10.3273281,4.07929253 10.5759739,4.52210317 10.7458178,5.00539515 Z M4,15 L10.5,15 C12.9852814,15 15,12.9852814 15,10.5 C15,8.01471863 12.9852814,6 10.5,6 C8.66116238,6 7.03779702,7.11297909 6.34791295,8.76123609 C7.34903712,9.48824913 8,10.6681043 8,12 C8,12.2761424 7.77614237,12.5 7.5,12.5 C7.22385763,12.5 7,12.2761424 7,12 C7,10.3431458 5.65685425,9 4,9 C2.34314575,9 1,10.3431458 1,12 C1,13.6568542 2.34314575,15 4,15 Z M9.69118222,5.05939327 C9.13621169,3.82983527 7.90059172,3 6.5,3 C4.56700338,3 3,4.56700338 3,6.5 C3,7.04681795 3.12529266,7.57427865 3.36125461,8.05072309 C3.56924679,8.01734375 3.78259797,8 4,8 C4.51800602,8 5.01301412,8.0984658 5.46732834,8.27770144 C6.22579284,6.55947236 7.82085903,5.33623457 9.69118222,5.05939327 Z" />
    </svg>
)

const ProductIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width="16"
        height="16"
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path d="M0 6.5l.004-.228c.039-1.186.678-2.31 1.707-2.956L6.293.57a3.512 3.512 0 013.414 0l4.582 2.746c1.029.646 1.668 1.77 1.707 2.956L16 6.5v3l-.004.228c-.039 1.186-.678 2.31-1.707 2.956l-4.582 2.746a3.512 3.512 0 01-3.414 0L1.711 12.684C.682 12.038.043 10.914.004 9.728L0 9.5v-3zM8 1.5a2.5 2.5 0 00-1.222.318L2.196 4.564A2.5 2.5 0 001 6.732V9.268a2.5 2.5 0 001.196 2.168l4.582 2.746a2.5 2.5 0 002.444 0l4.582-2.746A2.5 2.5 0 0015 9.268V6.732a2.5 2.5 0 00-1.196-2.168L9.222 1.818A2.5 2.5 0 008 1.5z" />
    </svg>
)

const VERSION_OPTIONS = [
    { id: '9.0+', label: '9.0+', current: true },
    { id: '8.19', label: '8.19', current: false },
    { id: '7.17', label: '7.17', current: false },
]

// Reusable facet header component
const FacetHeader = ({ icon, title }: { icon: ReactNode; title: string }) => {
    const { euiTheme } = useEuiTheme()

    return (
        <div
            css={css`
                display: flex;
                align-items: center;
                gap: ${euiTheme.size.s};
                padding: ${euiTheme.size.s} ${euiTheme.size.s};
                margin-bottom: ${euiTheme.size.xs};
                background: linear-gradient(
                    180deg,
                    ${euiTheme.colors.lightestShade} 0%,
                    transparent 100%
                );
                border-radius: ${euiTheme.border.radius.small};
                border-top: 1px solid ${euiTheme.colors.lightShade};
            `}
        >
            <span
                css={css`
                    color: ${euiTheme.colors.darkShade};
                    display: flex;
                `}
            >
                {icon}
            </span>
            <EuiText
                size="s"
                css={css`
                    color: ${euiTheme.colors.title};
                `}
            >
                <strong>{title}</strong>
            </EuiText>
        </div>
    )
}

// Icon for Type facet (grid/category icon)
const TypeIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width="16"
        height="16"
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path d="M6 2H2v4h4V2ZM1 1h6v6H1V1ZM14 2h-4v4h4V2Zm-5-1h6v6H9V1ZM6 10H2v4h4v-4ZM1 9h6v6H1V9ZM14 10h-4v4h4v-4Zm-5-1h6v6H9V9Z" />
    </svg>
)

// Type icons matching ResultCard
const TYPE_ICONS: Record<string, ReactNode> = {
    doc: <DocumentIcon />,
    api: <ConsoleIcon />,
}

// Label mappings for facet values
const TYPE_LABELS: Record<string, string> = {
    doc: 'Documentation',
    api: 'OpenAPI Reference',
}

interface VersionFilterProps {
    selected: string
    onChange: (version: string) => void
}

const VersionFilter = ({ selected, onChange }: VersionFilterProps) => {
    const { euiTheme } = useEuiTheme()

    return (
        <div
            css={css`
                margin-bottom: ${euiTheme.size.l};
            `}
        >
            <FacetHeader icon={<PackageIcon />} title="Version" />
            <div
                css={css`
                    display: flex;
                    flex-direction: column;
                    border: 1px solid ${euiTheme.border.color};
                    border-radius: ${euiTheme.border.radius.medium};
                    overflow: hidden;
                    margin-top: ${euiTheme.size.xs};
                `}
            >
                {VERSION_OPTIONS.map((option, idx) => {
                    const isSelected = selected === option.id
                    return (
                        <button
                            key={option.id}
                            onClick={() => onChange(option.id)}
                            css={css`
                                display: flex;
                                align-items: center;
                                justify-content: space-between;
                                padding: ${euiTheme.size.s} ${euiTheme.size.m};
                                background: ${isSelected
                                    ? `${euiTheme.colors.primary}1A`
                                    : euiTheme.colors.emptyShade};
                                color: ${isSelected
                                    ? euiTheme.colors.primary
                                    : euiTheme.colors.text};
                                border: none;
                                border-bottom: ${idx <
                                VERSION_OPTIONS.length - 1
                                    ? `1px solid ${euiTheme.border.color}`
                                    : 'none'};
                                cursor: pointer;
                                font-size: 14px;
                                font-weight: ${isSelected
                                    ? euiTheme.font.weight.semiBold
                                    : euiTheme.font.weight.regular};
                                transition: background 0.15s ease;
                                text-align: left;

                                &:hover {
                                    background: ${isSelected
                                        ? `${euiTheme.colors.primary}26`
                                        : euiTheme.colors.lightestShade};
                                }
                            `}
                        >
                            <span>{option.label}</span>
                            {option.current && (
                                <EuiBadge color="warning">current</EuiBadge>
                            )}
                        </button>
                    )
                })}
            </div>
        </div>
    )
}

interface TypeFilterProps {
    items: Record<string, number>
    selected: string[]
    onChange: (value: string) => void
}

const TypeFilter = ({ items, selected, onChange }: TypeFilterProps) => {
    const { euiTheme } = useEuiTheme()
    const [showAll, setShowAll] = useState(false)

    const sortedItems = Object.entries(items).sort(([, a], [, b]) => b - a)
    const hasMore = sortedItems.length > ITEMS_TO_SHOW
    const displayedItems = showAll
        ? sortedItems
        : sortedItems.slice(0, ITEMS_TO_SHOW)

    if (sortedItems.length === 0) return null

    return (
        <div
            css={css`
                margin-bottom: ${euiTheme.size.l};
            `}
        >
            <FacetHeader icon={<TypeIcon />} title="Type" />
            <div
                css={css`
                    margin-top: ${euiTheme.size.xs};
                `}
            >
                {displayedItems.map(([key, count]) => {
                    const isSelected = selected.includes(key)
                    return (
                        <button
                            key={key}
                            onClick={() => onChange(key)}
                            css={css`
                                display: flex;
                                align-items: center;
                                gap: ${euiTheme.size.s};
                                width: 100%;
                                padding: ${euiTheme.size.s} ${euiTheme.size.s};
                                background: ${isSelected
                                    ? `${euiTheme.colors.primary}1A`
                                    : 'transparent'};
                                border: none;
                                border-radius: ${euiTheme.border.radius.small};
                                cursor: pointer;
                                text-align: left;
                                transition: background 0.15s ease;

                                &:hover {
                                    background: ${isSelected
                                        ? `${euiTheme.colors.primary}26`
                                        : euiTheme.colors.lightestShade};
                                }
                            `}
                        >
                            <span
                                css={css`
                                    color: ${isSelected
                                        ? euiTheme.colors.primary
                                        : euiTheme.colors.subduedText};
                                    display: flex;
                                `}
                            >
                                {TYPE_ICONS[key] ?? <DocumentIcon />}
                            </span>
                            <EuiText
                                size="s"
                                css={css`
                                    flex: 1;
                                    font-weight: ${isSelected
                                        ? euiTheme.font.weight.semiBold
                                        : euiTheme.font.weight.regular};
                                    color: ${isSelected
                                        ? euiTheme.colors.primary
                                        : 'inherit'};
                                `}
                            >
                                {TYPE_LABELS[key] ?? key}
                            </EuiText>
                            {isSelected ? (
                                <EuiIcon
                                    type="cross"
                                    size="s"
                                    color="primary"
                                />
                            ) : (
                                <EuiText size="xs" color="subdued">
                                    {count.toLocaleString()}
                                </EuiText>
                            )}
                        </button>
                    )
                })}
                {hasMore && (
                    <button
                        onClick={() => setShowAll(!showAll)}
                        css={css`
                            display: flex;
                            align-items: center;
                            gap: ${euiTheme.size.xs};
                            width: 100%;
                            padding: ${euiTheme.size.xs} ${euiTheme.size.s};
                            background: none;
                            border: none;
                            cursor: pointer;
                            color: ${euiTheme.colors.primary};
                            font-size: 13px;
                            margin-top: ${euiTheme.size.xs};

                            &:hover {
                                text-decoration: underline;
                            }
                        `}
                    >
                        <EuiIcon
                            type={showAll ? 'arrowUp' : 'arrowDown'}
                            size="s"
                        />
                        {showAll
                            ? 'Show less'
                            : `Show ${sortedItems.length - ITEMS_TO_SHOW} more`}
                    </button>
                )}
            </div>
        </div>
    )
}

interface FacetFilterProps {
    title: string
    icon: ReactNode
    items: Record<string, number>
    selected: string[]
    onChange: (value: string) => void
}

const FacetFilter = ({
    title,
    icon,
    items,
    selected,
    onChange,
}: FacetFilterProps) => {
    const { euiTheme } = useEuiTheme()
    const [showAll, setShowAll] = useState(false)

    const sortedItems = Object.entries(items).sort(([, a], [, b]) => b - a)
    const hasMore = sortedItems.length > ITEMS_TO_SHOW
    const displayedItems = showAll
        ? sortedItems
        : sortedItems.slice(0, ITEMS_TO_SHOW)

    if (sortedItems.length === 0) return null

    return (
        <div
            css={css`
                margin-bottom: ${euiTheme.size.l};
            `}
        >
            <FacetHeader icon={icon} title={title} />
            <div
                css={css`
                    margin-top: ${euiTheme.size.xs};
                `}
            >
                {displayedItems.map(([key, count]) => {
                    const isSelected = selected.includes(key)
                    return (
                        <button
                            key={key}
                            onClick={() => onChange(key)}
                            css={css`
                                display: flex;
                                align-items: center;
                                gap: ${euiTheme.size.s};
                                width: 100%;
                                padding: ${euiTheme.size.s} ${euiTheme.size.s};
                                background: ${isSelected
                                    ? `${euiTheme.colors.primary}1A`
                                    : 'transparent'};
                                border: none;
                                border-radius: ${euiTheme.border.radius.small};
                                cursor: pointer;
                                text-align: left;
                                transition: background 0.15s ease;

                                &:hover {
                                    background: ${isSelected
                                        ? `${euiTheme.colors.primary}26`
                                        : euiTheme.colors.lightestShade};
                                }
                            `}
                        >
                            <EuiText
                                size="s"
                                css={css`
                                    flex: 1;
                                    text-transform: capitalize;
                                    font-weight: ${isSelected
                                        ? euiTheme.font.weight.semiBold
                                        : euiTheme.font.weight.regular};
                                    color: ${isSelected
                                        ? euiTheme.colors.primary
                                        : 'inherit'};
                                `}
                            >
                                {key}
                            </EuiText>
                            {isSelected ? (
                                <EuiIcon
                                    type="cross"
                                    size="s"
                                    color="primary"
                                />
                            ) : (
                                <EuiText size="xs" color="subdued">
                                    {count.toLocaleString()}
                                </EuiText>
                            )}
                        </button>
                    )
                })}
                {hasMore && (
                    <button
                        onClick={() => setShowAll(!showAll)}
                        css={css`
                            display: flex;
                            align-items: center;
                            gap: ${euiTheme.size.xs};
                            width: 100%;
                            padding: ${euiTheme.size.xs} ${euiTheme.size.s};
                            background: none;
                            border: none;
                            cursor: pointer;
                            color: ${euiTheme.colors.primary};
                            font-size: 13px;
                            margin-top: ${euiTheme.size.xs};

                            &:hover {
                                text-decoration: underline;
                            }
                        `}
                    >
                        <EuiIcon
                            type={showAll ? 'arrowUp' : 'arrowDown'}
                            size="s"
                        />
                        {showAll
                            ? 'Show less'
                            : `Show ${sortedItems.length - ITEMS_TO_SHOW} more`}
                    </button>
                )}
            </div>
        </div>
    )
}

interface ProductFilterProps {
    items: Record<string, number>
    selected: string[]
    onChange: (value: string) => void
}

const ProductFilterItem = ({
    productId,
    count,
    isSelected,
    onChange,
}: {
    productId: string
    count: number
    isSelected: boolean
    onChange: (value: string) => void
}) => {
    const { euiTheme } = useEuiTheme()
    return (
        <button
            onClick={() => onChange(productId)}
            css={css`
                display: flex;
                align-items: center;
                gap: ${euiTheme.size.s};
                width: 100%;
                padding: ${euiTheme.size.s} ${euiTheme.size.s};
                background: ${isSelected
                    ? `${euiTheme.colors.primary}1A`
                    : 'transparent'};
                border: none;
                border-radius: ${euiTheme.border.radius.small};
                cursor: pointer;
                text-align: left;
                transition: background 0.15s ease;

                &:hover {
                    background: ${isSelected
                        ? `${euiTheme.colors.primary}26`
                        : euiTheme.colors.lightestShade};
                }
            `}
        >
            <EuiText
                size="s"
                css={css`
                    flex: 1;
                    font-weight: ${isSelected
                        ? euiTheme.font.weight.semiBold
                        : euiTheme.font.weight.regular};
                    color: ${isSelected ? euiTheme.colors.primary : 'inherit'};
                `}
            >
                {getProductDisplayName(productId)}
            </EuiText>
            {isSelected ? (
                <EuiIcon type="cross" size="s" color="primary" />
            ) : (
                <EuiText size="xs" color="subdued">
                    {count.toLocaleString()}
                </EuiText>
            )}
        </button>
    )
}

const ProductFilter = ({ items, selected, onChange }: ProductFilterProps) => {
    const { euiTheme } = useEuiTheme()
    const [showAll, setShowAll] = useState(false)

    // Top 5 sorted by count (descending)
    const sortedByCount = Object.entries(items).sort(([, a], [, b]) => b - a)
    const topItems = sortedByCount.slice(0, ITEMS_TO_SHOW)

    // Others sorted alphabetically by display name
    const otherItems = sortedByCount.slice(ITEMS_TO_SHOW).sort(([a], [b]) => {
        const nameA = getProductDisplayName(a).toLowerCase()
        const nameB = getProductDisplayName(b).toLowerCase()
        return nameA.localeCompare(nameB)
    })

    const hasMore = otherItems.length > 0

    if (sortedByCount.length === 0) return null

    return (
        <div
            css={css`
                margin-bottom: ${euiTheme.size.l};
            `}
        >
            <FacetHeader icon={<ProductIcon />} title="Product" />
            <div
                css={css`
                    margin-top: ${euiTheme.size.xs};
                `}
            >
                {topItems.map(([key, count]) => (
                    <ProductFilterItem
                        key={key}
                        productId={key}
                        count={count}
                        isSelected={selected.includes(key)}
                        onChange={onChange}
                    />
                ))}

                {showAll && otherItems.length > 0 && (
                    <>
                        <div
                            css={css`
                                display: flex;
                                align-items: center;
                                gap: ${euiTheme.size.s};
                                padding: ${euiTheme.size.s} ${euiTheme.size.s};
                                margin-top: ${euiTheme.size.xs};
                            `}
                        >
                            <div
                                css={css`
                                    flex: 1;
                                    height: 1px;
                                    background: ${euiTheme.border.color};
                                `}
                            />
                            <EuiText
                                size="xs"
                                color="subdued"
                                css={css`
                                    text-transform: uppercase;
                                    font-weight: ${euiTheme.font.weight
                                        .semiBold};
                                    letter-spacing: 0.5px;
                                `}
                            >
                                Others
                            </EuiText>
                            <div
                                css={css`
                                    flex: 1;
                                    height: 1px;
                                    background: ${euiTheme.border.color};
                                `}
                            />
                        </div>
                        {otherItems.map(([key, count]) => (
                            <ProductFilterItem
                                key={key}
                                productId={key}
                                count={count}
                                isSelected={selected.includes(key)}
                                onChange={onChange}
                            />
                        ))}
                    </>
                )}

                {hasMore && (
                    <button
                        onClick={() => setShowAll(!showAll)}
                        css={css`
                            display: flex;
                            align-items: center;
                            gap: ${euiTheme.size.xs};
                            width: 100%;
                            padding: ${euiTheme.size.xs} ${euiTheme.size.s};
                            background: none;
                            border: none;
                            cursor: pointer;
                            color: ${euiTheme.colors.primary};
                            font-size: 13px;
                            margin-top: ${euiTheme.size.xs};

                            &:hover {
                                text-decoration: underline;
                            }
                        `}
                    >
                        <EuiIcon
                            type={showAll ? 'arrowUp' : 'arrowDown'}
                            size="s"
                        />
                        {showAll
                            ? 'Show less'
                            : `Show ${otherItems.length} more`}
                    </button>
                )}
            </div>
        </div>
    )
}

interface FilterSidebarProps {
    aggregations?: SearchAggregations
    version: string
    filters: FullPageSearchFilters
    onVersionChange: (version: string) => void
    onFilterChange: (key: keyof FullPageSearchFilters, value: string) => void
}

export const FilterSidebar = ({
    aggregations,
    version,
    filters,
    onVersionChange,
    onFilterChange,
}: FilterSidebarProps) => {
    return (
        <>
            <VersionFilter selected={version} onChange={onVersionChange} />

            <ProductFilter
                items={aggregations?.product ?? {}}
                selected={filters.product}
                onChange={(v) => onFilterChange('product', v)}
            />

            <TypeFilter
                items={aggregations?.type ?? {}}
                selected={filters.type}
                onChange={(v) => onFilterChange('type', v)}
            />

            <FacetFilter
                title="Section"
                icon={<LayersIcon />}
                items={aggregations?.navigationSection ?? {}}
                selected={filters.navigationSection}
                onChange={(v) => onFilterChange('navigationSection', v)}
            />

            <FacetFilter
                title="Deployment"
                icon={<CloudIcon />}
                items={aggregations?.deploymentType ?? {}}
                selected={filters.deploymentType}
                onChange={(v) => onFilterChange('deploymentType', v)}
            />
        </>
    )
}

// Export icons for use in ResultCard
export { DocumentIcon, ConsoleIcon }
