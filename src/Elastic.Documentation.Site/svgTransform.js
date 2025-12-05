/* global require, module */
/* eslint-disable @typescript-eslint/no-require-imports */
'use strict'

/**
 * Jest transformer for SVG files.
 * Converts SVG files into React components using @svgr/core,
 * matching the behavior of @parcel/transformer-svg-react.
 *
 * Based on: https://stackoverflow.com/questions/58603201/jest-cannot-load-svg-file
 */

const { transform } = require('@svgr/core')

module.exports = {
  process(sourceText, sourcePath) {
    // Use @svgr/core to transform SVG to React component
    const jsxCode = transform.sync(sourceText, {
      plugins: ['@svgr/plugin-jsx'],
      jsxRuntime: 'automatic',
      exportType: 'default',
    })

    // Transform JSX to JS with Babel
    const babel = require('@babel/core')
    const result = babel.transformSync(jsxCode, {
      presets: [
        ['@babel/preset-env', { targets: { node: 'current' } }],
        ['@babel/preset-react', { runtime: 'automatic' }],
      ],
      filename: sourcePath,
    })

    // The Babel output uses `exports.default = SvgComponent`
    // We need to add __esModule flag and module.exports for proper interop
    // See: https://stackoverflow.com/a/77814587
    const code = `
${result.code}
module.exports = exports.default;
module.exports.default = exports.default;
module.exports.__esModule = true;
`
    return { code }
  },

  getCacheKey() {
    return 'svgTransform-v2'
  },
}
