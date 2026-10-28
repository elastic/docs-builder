// The runtime the design-system bundle needs beyond the enhancement modules: htmx navigation, telemetry, code highlighting,
// math, diagrams, tables and CTA events. These are the framework-free parts of main.ts; the React islands and EUI loader are not
// part of the design-system bundle, so nothing here pulls them in.
import { initApiDocs } from '../api-docs'
import { initAppliesSwitch } from '../applies-switch'
import { initCopyButton } from '../copybutton'
import { initHighlight, preloadHighlight } from '../hljs'
import { initMath } from '../math'
import { initMermaid } from '../mermaid'
import { openDetailsWithAnchor } from '../open-details-with-anchor'
import { initPrivacyConsent } from '../privacy-consent'
import { bindCtaClicks, initCtaImpressions } from '../runtime/cta'
import { applyEditParam } from '../runtime/edit-param'
import { bindHtmxHooks } from '../runtime/htmx-hooks'
import { runInitSteps } from '../runtime/init-steps'
import { initTelemetry } from '../runtime/telemetry'
import { initSmoothScroll } from '../smooth-scroll'
import { initTable } from '../table'
import { initCodeLines } from './code-lines'
import 'htmx-ext-head-support'
import 'htmx-ext-preload'
import 'htmx.org'

const bootstrapPromise = initTelemetry()

async function awaitBootstrap() {
    await bootstrapPromise
}

// The languages chunk is the long pole for code pages; fetch it before the init steps get to it.
if (document.querySelector('pre code')) preloadHighlight()

// Initialize on initial page load
document.addEventListener('DOMContentLoaded', function () {
    runInitSteps([
        ['bootstrap', awaitBootstrap],
        ['initMath', initMath],
        ['initMermaid', initMermaid],
        ['initCtaImpressions', initCtaImpressions],
    ])
})

document.addEventListener('htmx:load', function () {
    runInitSteps([
        ['bootstrap', awaitBootstrap],
        ['initHighlight', initHighlight],
        ['initCodeLines', () => initCodeLines()],
        ['initCopyButton', initCopyButton],
        ['initAppliesSwitch', initAppliesSwitch],
        ['initMath', initMath],
        ['initMermaid', initMermaid],
        ['initSmoothScroll', initSmoothScroll],
        ['openDetailsWithAnchor', openDetailsWithAnchor],
        ['initTable', initTable],
        ['initApiDocs', initApiDocs],
        ['applyEditParam', applyEditParam],
        ['initCtaImpressions', initCtaImpressions],
    ])
})

bindCtaClicks()
initPrivacyConsent()
bindHtmxHooks()
