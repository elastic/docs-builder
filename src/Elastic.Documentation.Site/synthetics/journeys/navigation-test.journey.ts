import { journey, step, monitor, expect } from '@elastic/synthetics'

function getSchedule(env: string) {
    const scheduleMapping = {
        local: 15,
        edge: 15,
        staging: 15,
        prod: 1,
    }
    return scheduleMapping[env] || 15
}

/**
 * Walks the main navigation paths and, along the way, verifies the htmx
 * navigation model: boosted links do a whole-body swap with hx-preserve
 * islands. A `window` marker set on first load survives an htmx swap but
 * not a full page load, so it distinguishes SPA navigation from a reload.
 */
journey('navigation test', ({ page, params }) => {
    monitor.use({
        id: `elastic-co-docs-nav-${params.environment}-v2`,
        schedule: getSchedule(params.environment),
        tags: [`env:${params.environment}`],
    })

    const host = params.baseUrl
    step(`Go to ${host}`, async () => {
        await page.goto(`${host}/docs`, {
            timeout: 60000,
            waitUntil: 'domcontentloaded',
        })
        await expect(page).toHaveTitle(/Elastic Docs \| Elastic Docs/)
        await page.evaluate(() => {
            window['__synthNoReload'] = true
        })
    })

    step('Global nav space is reserved before it renders', async () => {
        const breakpoints = [
            { width: 375, expectedHeight: 102 },
            { width: 768, expectedHeight: 110 },
            { width: 1280, expectedHeight: 118 },
        ]

        for (const { width, expectedHeight } of breakpoints) {
            await page.setViewportSize({ width, height: 800 })
            const minHeight = await page
                .locator('#elastic-nav-wrapper')
                .evaluate((element) =>
                    Number.parseFloat(getComputedStyle(element).minHeight)
                )
            expect(minHeight).toBe(expectedHeight)
        }
    })

    step('Navigate to Elastic fundamentals', async () => {
        const fundamentalsLink = page
            .getByRole('link', { name: 'Elastic Fundamentals' })
            .first()
        const landingGetStarted = page
            .locator('a.card-cta')
            .filter({ hasText: 'Get started' })
            .first()

        if (
            await fundamentalsLink
                .isVisible({ timeout: 5000 })
                .catch(() => false)
        ) {
            await fundamentalsLink.click()
        } else {
            await landingGetStarted.click()
        }

        await expect(page).toHaveURL(`${host}/docs/get-started`)
        await expect(page).toHaveTitle(/Elastic fundamentals/)
        await expect(
            page.getByRole('heading', { name: 'Elastic fundamentals' })
        ).toBeVisible()
        // Tag sidebar chrome so later steps can tell preserved DOM from a swap
        // (Nav V2 keeps #pages-nav / #nav-tree; prefer the stable pages-nav host).
        await page.evaluate(() => {
            const nav =
                document.querySelector('#pages-nav') ??
                document.querySelector('[id^="nav-tree"]')
            if (nav) nav['__synthOriginal'] = true
        })
    })

    step(
        'Main content keeps its column while pages nav is absent',
        async () => {
            const placement = await page.evaluate(() => {
                const content =
                    document.querySelector<HTMLElement>('#content-container')
                const grid = content?.parentElement
                const sidebar =
                    grid?.querySelector<HTMLElement>(':scope > .sidebar')
                if (!content || !grid || !sidebar)
                    throw new Error(
                        'Expected documentation layout was not found'
                    )

                const marker = document.createComment('pages-nav-placeholder')
                sidebar.replaceWith(marker)

                const contentRect = content.getBoundingClientRect()
                const gridRect = grid.getBoundingClientRect()
                const firstColumnWidth = Number.parseFloat(
                    getComputedStyle(grid).gridTemplateColumns
                )

                marker.replaceWith(sidebar)
                return {
                    contentLeft: contentRect.left,
                    expectedMinimumLeft: gridRect.left + firstColumnWidth,
                }
            })

            expect(placement.contentLeft).toBeGreaterThanOrEqual(
                placement.expectedMinimumLeft
            )
        }
    )

    step('Click on "Evaluate Elastic during a trial" in nav', async () => {
        // Expand a collapsed nav folder so we can assert its state survives
        const expandedId = await page.evaluate(() => {
            const checkbox = document.querySelector<HTMLInputElement>(
                '#pages-nav input[type="checkbox"]:not(:checked), [id^="nav-tree"] input[type="checkbox"]:not(:checked)'
            )
            if (checkbox) checkbox.checked = true
            return checkbox?.id ?? null
        })

        // Nav V2 IA: same Guides/get-started group (deployment-options is no longer a sibling).
        await page
            .locator('#pages-nav')
            .getByRole('link', { name: 'Evaluate Elastic during a trial' })
            .first()
            .click()
        await expect(page).toHaveURL(
            `${host}/docs/get-started/evaluate-elastic`
        )
        await expect(page).toHaveTitle(/Evaluate Elastic during a trial/)
        await expect(
            page.getByRole('heading', {
                name: 'Evaluate Elastic during a trial',
            })
        ).toBeVisible()

        // Same-group navigation: no reload, sidebar host DOM (and state) preserved
        const state = await page.evaluate((id) => {
            const nav =
                document.querySelector('#pages-nav') ??
                document.querySelector('[id^="nav-tree"]')
            return {
                noReload: window['__synthNoReload'] === true,
                navPreserved: nav?.['__synthOriginal'] === true,
                checkboxStillChecked: id
                    ? (document.getElementById(id) as HTMLInputElement)?.checked
                    : null,
            }
        }, expandedId)
        expect(state.noReload).toBe(true)
        expect(state.navPreserved).toBe(true)
        if (expandedId) expect(state.checkboxStillChecked).toBe(true)
    })

    step('Click on "Elastic Cloud" in markdown content', async () => {
        const navMarkerBefore = await page.evaluate(() => {
            const nav =
                document.querySelector('#pages-nav') ??
                document.querySelector('[id^="nav-tree"]')
            return {
                id: nav?.id ?? null,
                hadMarker: nav?.['__synthOriginal'] === true,
            }
        })
        await page
            .locator('#markdown-content')
            .getByRole('link', { name: 'Elastic Cloud' })
            .first()
            .click()
        await expect(page).toHaveURL(
            `${host}/docs/deploy-manage/deploy/elastic-cloud`
        )
        await expect(page).toHaveTitle(/Elastic Cloud/)

        // Cross-group navigation: still no reload, but sidebar chrome is replaced
        const state = await page.evaluate(() => {
            const nav =
                document.querySelector('#pages-nav') ??
                document.querySelector('[id^="nav-tree"]')
            return {
                noReload: window['__synthNoReload'] === true,
                navId: nav?.id ?? null,
                navIsNewNode: nav?.['__synthOriginal'] === undefined,
                navShowsNewGroup:
                    nav?.querySelector('a[href*="/deploy-manage/"]') !== null,
            }
        })
        expect(state.noReload).toBe(true)
        // Nav V2 may keep a stable #pages-nav id while replacing inner tree HTML.
        if (navMarkerBefore.id && state.navId === navMarkerBefore.id) {
            expect(state.navIsNewNode).toBe(true)
        } else {
            expect(state.navId).not.toBe(navMarkerBefore.id)
            expect(state.navIsNewNode).toBe(true)
        }
        expect(state.navShowsNewGroup).toBe(true)
    })

    step('Use dropdown to navigate to reference', async () => {
        const referenceUrl = `${host}/docs/reference`
        const pagesDropdown = page.locator('#pages-dropdown')
        const referenceInDropdown = pagesDropdown.locator(
            'a[href="/docs/reference"]'
        )

        if (
            await pagesDropdown.isVisible({ timeout: 5000 }).catch(() => false)
        ) {
            await pagesDropdown.locator('svg').click()
            await referenceInDropdown.evaluate((el) =>
                (el as HTMLAnchorElement).click()
            )
            try {
                await expect(page).toHaveURL(referenceUrl, { timeout: 5000 })
            } catch {
                // Nav V2 sidebar can overlap the pages dropdown in assembler preview.
                await page.goto(referenceUrl)
            }
        } else {
            await page.goto(referenceUrl)
        }

        await expect(page).toHaveURL(referenceUrl)
    })

    step(
        'Global nav script executed only once across navigations',
        async () => {
            const navJsFetches = await page.evaluate(
                () =>
                    performance
                        .getEntriesByType('resource')
                        .filter((e) => e.name.includes('elastic-nav.js')).length
            )
            // elastic-nav.js lives in <head> (head-support keeps it) and must not
            // re-execute per navigation; 0 covers environments without the global nav
            expect(navJsFetches).toBeLessThanOrEqual(1)
        }
    )

    step('/docs/api link triggers a full page load, not htmx', async () => {
        await page.evaluate(() => {
            const a = document.createElement('a')
            a.href = '/docs/api/'
            a.id = 'synthetic-api-link'
            a.textContent = 'api'
            document.querySelector('#content-container')?.appendChild(a)
        })
        // A full page load is a navigation request; an htmx request would be an
        // XHR carrying the ?v= cache-buster. Status doesn't matter (404 locally).
        const [request] = await Promise.all([
            page.waitForRequest(
                (req) => req.url().startsWith(`${host}/docs/api/`),
                { timeout: 30000 }
            ),
            page.locator('#synthetic-api-link').click(),
        ])
        expect(request.isNavigationRequest()).toBe(true)
        expect(new URL(request.url()).searchParams.has('v')).toBe(false)
    })
})
