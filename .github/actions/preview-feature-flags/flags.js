// Maps `preview:<flag>` pull request labels to the FEATURE_* variables of a preview build.
// Plain CommonJS with no dependencies, so `action.yml` can require it and a shell can run it with node.
'use strict'

const labelPrefix = 'preview:'

/** The kebab-case keys that `FeatureFlags.KnownKeys` lists, read from the C# source so the two cannot drift. */
function knownKeysFrom(source) {
    const start = source.indexOf('KnownKeys')
    const end = start < 0 ? -1 : source.indexOf('];', start)
    if (end < 0) throw new Error('FeatureFlags.cs no longer declares KnownKeys as a list literal')
    return [...source.slice(start, end).matchAll(/"([a-z0-9-]+)"/g)].map((m) => m[1])
}

/** The FEATURE_* names the labels ask for, and the labels that name no known flag. */
function flagsFromLabels(labels, knownKeys) {
    const enabled = new Set()
    const unknown = []
    for (const label of labels) {
        if (!label.startsWith(labelPrefix)) continue
        const key = label.slice(labelPrefix.length).trim().toLowerCase()
        if (knownKeys.includes(key)) enabled.add(`FEATURE_${key.toUpperCase().replace(/-/g, '_')}`)
        else unknown.push(label)
    }
    return { enabled: [...enabled].sort(), unknown }
}

module.exports = { labelPrefix, knownKeysFrom, flagsFromLabels }
