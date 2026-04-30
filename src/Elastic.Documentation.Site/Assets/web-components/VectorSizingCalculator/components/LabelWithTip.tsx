import { EuiIcon, EuiToolTip } from '@elastic/eui'
import type { ReactNode } from 'react'

interface LabelWithTipProps {
    children: ReactNode
    tip: string
}

export function LabelWithTip({ children, tip }: LabelWithTipProps) {
    return (
        <EuiToolTip content={tip} position="right">
            <span className="vectorSizingCalc__labelRow">
                <span>{children}</span>
                <span className="vectorSizingCalc__labelTip" aria-hidden="true">
                    <EuiIcon type="info" />
                </span>
            </span>
        </EuiToolTip>
    )
}
