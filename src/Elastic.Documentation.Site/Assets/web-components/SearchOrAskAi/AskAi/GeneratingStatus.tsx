/** @jsxImportSource @emotion/react */
import {
    EuiFlexGroup,
    EuiFlexItem,
    EuiLoadingSpinner,
    EuiText,
} from '@elastic/eui'
import * as React from 'react'

interface GeneratingStatusProps {
    status: string | null
}

export const GeneratingStatus = ({ status }: GeneratingStatusProps) => {
    if (!status) {
        return null
    }

    return (
        <EuiFlexGroup alignItems="center" gutterSize="s" responsive={false}>
            <EuiFlexItem grow={false}>
                <EuiLoadingSpinner size="s" />
            </EuiFlexItem>
            <EuiFlexItem grow={false}>
                <EuiText size="xs" color="subdued">
                    {status}...
                </EuiText>
            </EuiFlexItem>
        </EuiFlexGroup>
    )
}
