import '../../eui-icons-cache'
import { Calculator } from './Calculator'
import { EuiProvider } from '@elastic/eui'
import r2wc from '@r2wc/react-to-web-component'
import * as React from 'react'
import { StrictMode } from 'react'

const S3CostCalculatorWrapper = () => {
    return (
        <StrictMode>
            <EuiProvider
                colorMode="light"
                globalStyles={false}
                utilityClasses={false}
            >
                <Calculator />
            </EuiProvider>
        </StrictMode>
    )
}

if (!customElements.get('s3-cost-calculator')) {
    customElements.define('s3-cost-calculator', r2wc(S3CostCalculatorWrapper))
}
