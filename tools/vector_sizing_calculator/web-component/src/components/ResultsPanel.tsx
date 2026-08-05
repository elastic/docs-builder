import {
  EuiPanel,
  EuiFlexGroup,
  EuiFlexItem,
  EuiSpacer,
  EuiStat,
  EuiText,
  EuiTitle,
  EuiHorizontalRule,
} from '@elastic/eui';
import type { SizingResult } from '../types';
import { formatBytes } from '../calculations';
import { BreakdownChart } from './BreakdownChart';

interface ResultsPanelProps {
  result: SizingResult | null;
}

export function ResultsPanel({ result }: ResultsPanelProps) {
  const diskFmt = result ? formatBytes(result.totalDisk) : null;
  const ramFmt = result ? formatBytes(result.totalRam) : null;

  return (
    <EuiPanel paddingSize="l">
      <EuiTitle size="xxs">
        <h3>Estimated Requirements (per replica)</h3>
      </EuiTitle>
      <EuiHorizontalRule margin="s" />

      <EuiFlexGroup gutterSize="l">
        <EuiFlexItem>
          <EuiStat
            title={result ? `${diskFmt!.value} ${diskFmt!.unit}` : '—'}
            description="Total Disk"
            titleColor="primary"
            titleSize="m"
          >
            {result && (
              <EuiText size="xs" color="subdued">
                {result.totalDisk.toLocaleString('en-US')} bytes
              </EuiText>
            )}
          </EuiStat>
        </EuiFlexItem>
        <EuiFlexItem>
          <EuiStat
            title={result ? `${ramFmt!.value} ${ramFmt!.unit}` : '—'}
            description="Off-Heap RAM"
            titleColor="primary"
            titleSize="m"
          >
            {result && (
              <EuiText size="xs" color="subdued">
                {result.totalRam.toLocaleString('en-US')} bytes
              </EuiText>
            )}
          </EuiStat>
        </EuiFlexItem>
      </EuiFlexGroup>

      {result && (
        <>
          <EuiSpacer size="l" />
          <BreakdownChart
            title="Disk Breakdown"
            items={result.diskBreakdown}
            total={result.totalDisk}
          />
          <EuiSpacer size="l" />
          <BreakdownChart
            title="Off-Heap RAM Breakdown"
            items={result.ramBreakdown}
            total={result.totalRam}
          />
        </>
      )}
    </EuiPanel>
  );
}
