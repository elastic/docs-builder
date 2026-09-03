import { formatDiskToRamSentence, formatTimes } from './formatNumbers'

describe('formatTimes', () => {
    it('uses no decimal places at 10× and above', () => {
        expect(formatTimes(19.4)).toBe('19×')
        expect(formatTimes(10)).toBe('10×')
    })

    it('keeps one decimal place below 10×', () => {
        expect(formatTimes(1.54)).toBe('1.5×')
        expect(formatTimes(2.3)).toBe('2.3×')
    })
})

describe('formatDiskToRamSentence', () => {
    it('describes disk relative to the off-heap RAM working set', () => {
        expect(formatDiskToRamSentence(19.4)).toBe(
            'Disk is about 19× the off-heap RAM working set.'
        )
        expect(formatDiskToRamSentence(1.54)).toBe(
            'Disk is about 1.5× the off-heap RAM working set.'
        )
    })

    it('returns empty when the ratio is not meaningful', () => {
        expect(formatDiskToRamSentence(0)).toBe('')
        expect(formatDiskToRamSentence(-1)).toBe('')
    })
})
