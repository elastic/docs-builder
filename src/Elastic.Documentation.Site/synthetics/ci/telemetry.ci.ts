import { expect, journey, step } from '@elastic/synthetics'
import type { Page } from 'playwright-core'

/**
 * Runs the deployed site with the EDOT browser SDK on, in a real browser, and checks that telemetry is
 * exported and that the SDK does not break the page.
 *
 * Unit tests run the SDK in jsdom and the other journeys run with it off: the synthetics browser sends the
 * `Elastic/Synthetics` user agent, which turns the SDK off (`isSyntheticMonitor` in
 * `Assets/telemetry/instrumentation.ts`). This journey opens its own browser context with a normal user agent
 * instead. It lives in `ci/`, not `journeys/`, because it sends telemetry: `synthetics:push` would otherwise
 * publish it as a monitor that exports from two locations every 15 minutes.
 *
 * Run it against a deployed environment that has a collector: `npm run synthetics:test:telemetry:staging`.
 */

// The marker at the end lets the test's own traffic be told apart in Elasticsearch.
const USER_AGENT =
    'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 docs-builder-telemetry-test'

// Environments that have a collector the browser can post to.
const ENVIRONMENTS_WITH_COLLECTOR = ['staging', 'edge']

// Well above a normal batch. Resource timing used to push batches past 60 KB, so a regression shows here.
const MAX_BODY_BYTES = 32 * 1024

type TelemetryExport = {
    signal: 'traces' | 'logs' | 'metrics'
    status: number
    bytes: number
    body: OtlpBody
}

type OtlpAttribute = { key: string; value: unknown }
type OtlpBody = {
    resourceLogs?: Array<{
        resource: { attributes?: OtlpAttribute[] }
        scopeLogs: Array<{
            logRecords: Array<{
                eventName?: string
                attributes?: OtlpAttribute[]
            }>
        }>
    }>
    resourceSpans?: Array<{ resource: { attributes?: OtlpAttribute[] } }>
}

const SIGNAL_BY_PATH_SUFFIX = { t: 'traces', l: 'logs', m: 'metrics' } as const

function attributeValue(attributes: OtlpAttribute[] | undefined, key: string) {
    const value = attributes?.find((a) => a.key === key)?.value as
        { stringValue?: string } | undefined
    return value?.stringValue
}

async function waitFor(condition: () => boolean, timeoutMs: number) {
    const deadline = Date.now() + timeoutMs
    while (!condition() && Date.now() < deadline) {
        await new Promise((resolve) => setTimeout(resolve, 250))
    }
    return condition()
}

journey('browser telemetry', ({ browser, params }) => {
    const docsRoot = params.docsRoot as string
    const environment = params.environment as string

    if (!ENVIRONMENTS_WITH_COLLECTOR.includes(environment)) {
        console.log(
            `Skipping the telemetry journey: ${environment} has no collector to post to`
        )
        return
    }

    const exports: TelemetryExport[] = []
    const pageErrors: string[] = []
    const sdkChunks = new Set<string>()
    let page: Page

    const logRecords = () =>
        exports
            .filter((e) => e.signal === 'logs')
            .flatMap((e) => e.body.resourceLogs ?? [])
            .flatMap((r) => r.scopeLogs)
            .flatMap((s) => s.logRecords)

    step('Open the site with the SDK on', async () => {
        const context = await browser.newContext({ userAgent: USER_AGENT })
        page = await context.newPage()

        page.on('pageerror', (error) => pageErrors.push(error.message))
        page.on('request', (request) => {
            const match = /\/(chunk-instrumentation-[^/?]+\.js)/.exec(
                request.url()
            )
            if (match) sdkChunks.add(match[1])
        })
        page.on('response', async (response) => {
            const request = response.request()
            const match = /\/_api\/v1\/o\/([tlm])$/.exec(
                new URL(response.url()).pathname
            )
            if (request.method() !== 'POST' || !match) return
            const text = request.postData() ?? ''
            exports.push({
                signal: SIGNAL_BY_PATH_SUFFIX[match[1]],
                status: response.status(),
                bytes: text.length,
                body: JSON.parse(text || '{}') as OtlpBody,
            })
        })

        await page.goto(docsRoot, { timeout: 60000, waitUntil: 'networkidle' })

        expect(
            sdkChunks.size,
            'The SDK chunk was never requested, so telemetry is off in this build'
        ).toBeGreaterThan(0)
    })

    step('Navigation still works with the SDK on', async () => {
        await page.evaluate(() => {
            window['__synthNoReload'] = true
        })
        await page
            .getByRole('link', { name: 'Elastic Fundamentals' })
            .first()
            .click()
        await expect(page).toHaveURL(/\/get-started/)
        // The marker survives an htmx swap but not a full page load: the SDK did not break boosted navigation.
        expect(await page.evaluate(() => window['__synthNoReload'])).toBe(true)
        await expect(page.locator('#main-container h1').first()).toBeVisible()
    })

    step('Telemetry reaches the collector', async () => {
        // Leaving the page makes the SDK flush what it has batched.
        await page.goto('about:blank')
        const arrived = await waitFor(
            () => exports.some((e) => e.signal === 'logs'),
            20000
        )
        expect(arrived, 'No log export was posted within 20 s').toBe(true)

        const failed = exports.filter((e) => e.status < 200 || e.status >= 300)
        expect(
            failed.map((e) => `${e.signal} -> ${e.status}`),
            'Every export must be accepted'
        ).toEqual([])
        expect(Math.max(...exports.map((e) => e.bytes))).toBeLessThan(
            MAX_BODY_BYTES
        )
    })

    step('The exports have the expected shape', async () => {
        const resourceAttributes = exports
            .flatMap((e) => e.body.resourceLogs ?? [])
            .map((r) => r.resource.attributes)
        expect(
            resourceAttributes.map((a) => attributeValue(a, 'service.name'))
        ).toContain('docs-frontend')
        expect(
            resourceAttributes.map((a) =>
                attributeValue(a, 'telemetry.distro.name')
            )
        ).toContain('elastic')

        const eventNames = logRecords().map((r) => r.eventName)
        expect(eventNames).toContain('browser.navigation_timing')
        // Resource timing logs a record per asset and is turned off.
        expect(eventNames).not.toContain('browser.resource_timing')

        const sessionIds = logRecords().map((r) =>
            attributeValue(r.attributes, 'session.id')
        )
        expect(sessionIds.filter(Boolean).length).toBeGreaterThan(0)
    })

    step('The page raised no errors', async () => {
        expect(pageErrors).toEqual([])
    })
})
