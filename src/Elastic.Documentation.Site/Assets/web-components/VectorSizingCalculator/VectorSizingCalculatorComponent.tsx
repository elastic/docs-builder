import '../../eui-icons-cache'
import { Calculator } from './Calculator'
import { EuiProvider } from '@elastic/eui'
import r2wc from '@r2wc/react-to-web-component'
import * as React from 'react'
import { StrictMode } from 'react'

const VectorSizingCalculatorWrapper = () => {
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

if (!customElements.get('vector-sizing-calculator')) {
    customElements.define(
        'vector-sizing-calculator',
        r2wc(VectorSizingCalculatorWrapper)
    )
}
