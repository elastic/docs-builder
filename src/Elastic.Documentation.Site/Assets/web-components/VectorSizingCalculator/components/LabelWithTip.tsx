import { CalcToolTip } from './CalcToolTip'
import type { ReactNode } from 'react'

interface LabelWithTipProps {
    children: ReactNode
    tip: ReactNode
}

export function LabelWithTip({ children, tip }: LabelWithTipProps) {
    return (
        <CalcToolTip content={tip} position="right">
            {children}
        </CalcToolTip>
    )
}
