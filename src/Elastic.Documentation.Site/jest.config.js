const isCI = process.env.CI === 'true'

module.exports = {
  testEnvironment: 'jsdom',
  setupFilesAfterEnv: ['<rootDir>/setupTests.ts'],
  transform: {
    '^.+\\.(ts|tsx)$': [
      'babel-jest',
      {
        presets: [
          ['@babel/preset-env', { targets: { node: 'current' } }],
          ['@babel/preset-react', { runtime: 'automatic' }],
          '@babel/preset-typescript',
        ],
      },
    ],
    '^.+\\.(js|jsx)$': [
      'babel-jest',
      {
        presets: [
          ['@babel/preset-env', { targets: { node: 'current' } }],
          ['@babel/preset-react', { runtime: 'automatic' }],
        ],
      },
    ],
  },
  transformIgnorePatterns: [],
  // Use 'summary' reporter in CI for cleaner logs, 'default' locally for verbose output
  reporters: isCI
    ? [['github-actions', { silent: false }], 'summary']
    : [['github-actions', { silent: false }], 'default'],
  // Verbose output locally, quiet in CI
  verbose: !isCI,
}
