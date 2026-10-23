import { HostName, hostPorts, viewports } from './pages'
import { defineConfig } from '@playwright/test'

// Hosts to run, e.g. VISUAL_HOSTS=isolated,assembler. The assembler needs `npm run visual:prepare` first. Codex runs only when
// VISUAL_CODEX_CONFIG names a config, because Codex content can be private.
const defaultHosts = ['isolated', 'assembler']
if (process.env.VISUAL_CODEX_CONFIG) defaultHosts.push('codex')
const hosts = (process.env.VISUAL_HOSTS ?? defaultHosts.join(','))
    .split(',')
    .map((h) => h.trim()) as HostName[]

export default defineConfig({
    testDir: '.',
    testMatch: '**/*.visual.ts',
    snapshotPathTemplate:
        '{testDir}/__snapshots__/{projectName}/{arg}-{platform}{ext}',
    fullyParallel: true,
    reporter: [['list'], ['html', { open: 'never' }]],
    expect: {
        toHaveScreenshot: { maxDiffPixelRatio: 0.002 },
    },
    projects: hosts.flatMap((host) =>
        Object.entries(viewports).map(([viewport, size]) => ({
            name: `${host}-${viewport}`,
            metadata: { host },
            use: {
                baseURL: `http://localhost:${hostPorts[host]}`,
                viewport: size,
                browserName: 'chromium' as const,
            },
        }))
    ),
    webServer: hosts.map((host) => ({
        command: `node host.mjs ${host} ${hostPorts[host]}`,
        url: `http://localhost:${hostPorts[host]}`,
        reuseExistingServer: !!process.env.VISUAL_REUSE,
        // A host that fails to start explains itself in the test output.
        stdout: 'pipe',
        stderr: 'pipe',
        timeout: 180_000,
    })),
})
