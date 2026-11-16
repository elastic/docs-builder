/**
 * @vitest-environment node
 */
import type { UserConfig } from 'vite'

vi.mock('vite', () => ({ defineConfig: (config: unknown) => config }))
vi.mock('@tailwindcss/vite', () => ({
    default: () => ({ name: 'tailwindcss' }),
}))
vi.mock('@vitejs/plugin-react', () => ({ default: () => ({ name: 'react' }) }))
vi.mock('vite-plugin-svgr', () => ({ default: () => ({ name: 'svgr' }) }))

const loadConfig = async (argv: string[]): Promise<UserConfig> => {
    const originalArgv = process.argv
    process.argv = ['node', 'vite', ...argv]
    try {
        vi.resetModules()
        return (await import('./vite.config')).default as UserConfig
    } finally {
        process.argv = originalArgv
    }
}

describe('vite.config build.watch', () => {
    it('leaves watch unset for a one-shot build so `vite build` exits', async () => {
        const config = await loadConfig(['build'])

        expect(config.build?.watch).toBeNull()
    })

    it('excludes the build output under --watch so writes do not trigger a rebuild', async () => {
        const config = await loadConfig(['build', '--watch'])

        expect(config.build?.watch).toEqual(
            expect.objectContaining({
                exclude: expect.arrayContaining(['**/_static/**']),
            })
        )
    })
})
