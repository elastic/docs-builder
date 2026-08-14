import AxeBuilder from '@axe-core/playwright'
import { journey, step } from '@elastic/synthetics'

const docsPaths = [
    '/docs',
    '/docs/get-started',
    '/docs/get-started/deployment-options',
    '/docs/deploy-manage/deploy/elastic-cloud',
    '/docs/reference',
]

const wcagTags = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']

type AxeViolation = Awaited<
    ReturnType<AxeBuilder['analyze']>
>['violations'][number]

function escapeWorkflowCommand(value: string) {
    return value
        .replaceAll('%', '%25')
        .replaceAll('\r', '%0D')
        .replaceAll('\n', '%0A')
}

function escapeWorkflowProperty(value: string) {
    return escapeWorkflowCommand(value)
        .replaceAll(':', '%3A')
        .replaceAll(',', '%2C')
}

function reportViolations(path: string, violations: AxeViolation[]) {
    if (violations.length === 0) {
        console.log(`No accessibility violations found on ${path}`)
        return
    }

    for (const violation of violations) {
        const selectors = violation.nodes
            .map((node) => JSON.stringify(node.target))
            .join(', ')
        const message = [
            `${path}: ${violation.help}`,
            `Impact: ${violation.impact ?? 'unknown'}`,
            `Help: ${violation.helpUrl}`,
            `Selectors: ${selectors}`,
        ].join(' | ')
        const title = `Accessibility ${violation.id}`

        console.warn(
            `::warning title=${escapeWorkflowProperty(title)}::${escapeWorkflowCommand(message)}`
        )
    }
}

journey('CI accessibility audit', ({ page, params }) => {
    for (const path of docsPaths) {
        step(`Audit ${path}`, async () => {
            await page.goto(`${params.baseUrl}${path}`, {
                timeout: 60000,
                waitUntil: 'load',
            })

            const results = await new AxeBuilder({ page })
                .withTags(wcagTags)
                .analyze()

            reportViolations(path, results.violations)
        })
    }
})
