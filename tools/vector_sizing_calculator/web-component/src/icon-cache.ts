/**
 * Pre-cache EUI icons used by the calculator.
 *
 * EUI dynamically imports icon SVGs at runtime, which fails in Vite's
 * dev server. Statically importing them here and registering via
 * appendIconComponentCache avoids the dynamic import entirely.
 */
import { appendIconComponentCache } from '@elastic/eui/es/components/icon/icon';

import { icon as arrowDown } from '@elastic/eui/es/components/icon/assets/arrow_down';
import { icon as arrowRight } from '@elastic/eui/es/components/icon/assets/arrow_right';
import { icon as arrowUp } from '@elastic/eui/es/components/icon/assets/arrow_up';
import { icon as warning } from '@elastic/eui/es/components/icon/assets/warning';
import { icon as iInCircle } from '@elastic/eui/es/components/icon/assets/iInCircle';
import { icon as check } from '@elastic/eui/es/components/icon/assets/check';
import { icon as copy } from '@elastic/eui/es/components/icon/assets/copy';
import { icon as copyClipboard } from '@elastic/eui/es/components/icon/assets/copy_clipboard';
import { icon as crossInCircle } from '@elastic/eui/es/components/icon/assets/cross_in_circle';

appendIconComponentCache({
  arrowDown,
  arrowRight,
  arrowUp,
  warning,
  iInCircle,
  check,
  copy,
  copyClipboard,
  crossInCircleFilled: crossInCircle,
});
