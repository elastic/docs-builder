import { logError } from '../telemetry/logging'

// Run each init step in isolation so a failure in one does not abort the rest.
export async function runInitSteps(
    steps: Array<[string, () => void | Promise<void>]>
) {
    for (const [name, init] of steps) {
        try {
            await init()
        } catch (error) {
            console.error(`Init step "${name}" failed:`, error)
            logError(`Init step failed: ${name}`, {
                'init.step': name,
                'error.message':
                    error instanceof Error ? error.message : String(error),
            })
        }
    }
}
