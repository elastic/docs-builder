export function parseVectorCount(s: string): number {
  if (!s) return NaN;
  const clean = s.trim().replace(/,/g, '');
  const multipliers: Record<string, number> = {
    k: 1e3, K: 1e3, m: 1e6, M: 1e6, b: 1e9, B: 1e9,
  };
  const match = clean.match(/^(\d+\.?\d*)\s*([kKmMbB])?$/);
  if (!match) return parseInt(clean, 10);
  return Math.round(
    parseFloat(match[1]) * (match[2] ? multipliers[match[2]] : 1)
  );
}
