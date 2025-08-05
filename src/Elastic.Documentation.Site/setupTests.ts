import '@testing-library/jest-dom'

// This prevets the error:
// TypeError: Cannot set properties of undefined (setting 'diff')
jest.mock('@elastic/eui/lib/components/text_diff', () => ({
    EuiTextDiff: () => null,
}))
