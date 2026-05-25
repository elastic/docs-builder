import { EuiIcon, EuiText, EuiToolTip } from '@elastic/eui'
import classNames from 'classnames'
import type { ReactNode } from 'react'

type ToolTipPosition = 'top' | 'right' | 'bottom' | 'left'

interface CalcToolTipProps {
    content: ReactNode
    children: ReactNode
    position?: ToolTipPosition
    rowClassName?: string
    wrapperClassName?: string
    /** Enable when the anchor sits inside overflow/scroll containers (e.g. results panel). */
    repositionOnScroll?: boolean
}

function formatTooltipContent(content: ReactNode): ReactNode {
    if (typeof content === 'string') {
        return (
            <div className="vectorSizingCalc__tooltipBody">
                <EuiText size="xs" color="inherit">
                    {content}
                </EuiText>
            </div>
        )
    }

    return <div className="vectorSizingCalc__tooltipBody">{content}</div>
}

/** Shared info-icon tooltip used in settings labels and result byte sizes. */
export function CalcToolTip({
    content,
    children,
    position = 'right',
    rowClassName,
    wrapperClassName,
    repositionOnScroll = false,
}: CalcToolTipProps) {
    return (
        <span className={wrapperClassName}>
            <EuiToolTip
                content={formatTooltipContent(content)}
                position={position}
                display="inlineBlock"
                repositionOnScroll={repositionOnScroll}
            >
                <span
                    className={classNames(
                        'vectorSizingCalc__labelRow',
                        rowClassName
                    )}
                >
                    <span>{children}</span>
                    <span
                        className="vectorSizingCalc__labelTip"
                        aria-hidden="true"
                    >
                        <EuiIcon type="info" />
                    </span>
                </span>
            </EuiToolTip>
        </span>
    )
}
