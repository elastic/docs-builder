import type { ApiRoute } from './apiPropertyResults'
import { useEuiTheme } from '@elastic/eui'
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

/** The half-open ranges of `path` any token matches, ignoring case, merged where they touch. */
export const matchRanges = (
    path: string,
    tokens: string[]
): [number, number][] => {
    const haystack = path.toLowerCase()
    const found: [number, number][] = []
    for (const token of tokens) {
        const needle = token.toLowerCase()
        if (!needle) continue
        for (
            let at = haystack.indexOf(needle);
            at >= 0;
            at = haystack.indexOf(needle, at + needle.length)
        )
            found.push([at, at + needle.length])
    }
    return found
        .sort(([a], [b]) => a - b)
        .reduce<[number, number][]>((merged, [start, end]) => {
            const previous = merged[merged.length - 1]
            if (previous && start <= previous[1])
                previous[1] = Math.max(previous[1], end)
            else merged.push([start, end])
            return merged
        }, [])
}

/** Marks the slice of `ranges` that falls inside `text`, which starts `offset` characters into the path. */
const markRanges = (
    text: string,
    offset: number,
    ranges: [number, number][]
) => {
    const nodes: React.ReactNode[] = []
    let at = 0
    for (const [start, end] of ranges) {
        const from = Math.max(start - offset, at)
        const to = Math.min(end - offset, text.length)
        if (to <= from) continue
        if (from > at) nodes.push(text.slice(at, from))
        nodes.push(<mark key={from}>{text.slice(from, to)}</mark>)
        at = to
    }
    if (at < text.length) nodes.push(text.slice(at))
    return nodes
}

const MethodChip = ({ method }: { method: string }) => (
    <span className={`api-method api-method-${method.toLowerCase()}`}>
        <span className="api-method-label">{method}</span>
    </span>
)

/** One chip per method, joined into one when the route takes several, the method the page leads with first. */
const MethodChips = ({ methods }: { methods: string[] }) =>
    methods.length === 1 ? (
        <MethodChip method={methods[0]} />
    ) : (
        <span className="api-method-group">
            {methods.map((method) => (
                <MethodChip key={method} method={method} />
            ))}
        </span>
    )

/** The route a query named: every method it takes and the path. */
export const RouteLine = ({
    route: { path, methods },
    tokens,
}: {
    route: ApiRoute
    tokens: string[]
}) => {
    const { euiTheme } = useEuiTheme()
    const ranges = matchRanges(path, tokens)
    let offset = 0
    const spans: React.ReactNode[] = []
    for (const [name, text] of Object.entries(splitRoutePath(path))) {
        if (text)
            spans.push(
                <span key={name} className={`route-${name}`}>
                    {markRanges(text, offset, ranges)}
                </span>
            )
        offset += text.length
    }
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
                    font-size: 10px;
                }
                > .api-method,
                .api-method-group {
                    border-radius: ${euiTheme.size.xs};
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
            <MethodChips methods={methods} />
            <code title={path}>{spans}</code>
        </div>
    )
}
