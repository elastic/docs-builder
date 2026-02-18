// Type declarations for EUI internal icon imports (no public .d.ts files)
declare module '@elastic/eui/es/components/icon/icon' {
  export function appendIconComponentCache(map: Record<string, unknown>): void;
}

declare module '@elastic/eui/es/components/icon/assets/*' {
  import { ComponentType, SVGAttributes } from 'react';
  export const icon: ComponentType<SVGAttributes<SVGElement> & { title?: string; titleId?: string }>;
}
