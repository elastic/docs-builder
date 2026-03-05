import { config, type BuildType } from '../../../config'
import { assemblerStrategy } from './strategies/assembler'
import { codexStrategy } from './strategies/codex'
import { isolatedStrategy } from './strategies/isolated'
import type { HtmxUrlStrategy } from './strategies/types'

const strategies: Record<BuildType, HtmxUrlStrategy> = {
    assembler: assemblerStrategy,
    codex: codexStrategy,
    isolated: isolatedStrategy,
}

export const urlStrategy: HtmxUrlStrategy =
    strategies[config.buildType] ?? isolatedStrategy
