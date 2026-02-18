import {
  EuiFlexGroup,
  EuiFlexItem,
  EuiProgress,
  EuiText,
  EuiSpacer,
  EuiTitle,
} from '@elastic/eui';
import type { BreakdownItem } from '../types';
import { formatBytesString } from '../calculations';

interface BreakdownChartProps {
  title: string;
  items: BreakdownItem[];
  total: number;
}

export function BreakdownChart({ title, items, total }: BreakdownChartProps) {
  if (items.length === 0) return null;

  return (
    <>
      <EuiTitle size="xxxs">
        <h4>{title}</h4>
      </EuiTitle>
      <EuiSpacer size="s" />
      {items.map((item, idx) => {
        const pct = total > 0 ? (item.bytes / total) * 100 : 0;
        return (
          <div key={item.label}>
            <EuiFlexGroup alignItems="center" gutterSize="s" responsive={false}>
              <EuiFlexItem grow={false} style={{ width: 160, textAlign: 'right' }}>
                <EuiText size="xs" color="subdued">
                  {item.label}
                </EuiText>
              </EuiFlexItem>
              <EuiFlexItem>
                <EuiProgress
                  value={Math.max(pct, 0.5)}
                  max={100}
                  color={item.color}
                  size="m"
                />
              </EuiFlexItem>
              <EuiFlexItem grow={false} style={{ minWidth: 80 }}>
                <EuiText size="xs" color="subdued">
                  {formatBytesString(item.bytes)}
                </EuiText>
              </EuiFlexItem>
            </EuiFlexGroup>
            {idx < items.length - 1 && <EuiSpacer size="xs" />}
          </div>
        );
      })}
    </>
  );
}
