import {
  EuiPanel,
  EuiFlexGroup,
  EuiFlexItem,
  EuiStat,
  EuiText,
  EuiTitle,
  EuiHorizontalRule,
} from '@elastic/eui';
import type { SizingResult } from '../types';
import { formatBytes } from '../calculations';

interface ClusterTotalsProps {
  result: SizingResult;
  replicas: number;
}

export function ClusterTotals({ result, replicas }: ClusterTotalsProps) {
  const diskFmt = formatBytes(result.clusterDisk);
  const ramFmt = formatBytes(result.clusterRam);

  return (
    <EuiPanel paddingSize="l">
      <EuiTitle size="xxs">
        <h3>Cluster-Wide Totals</h3>
      </EuiTitle>
      <EuiHorizontalRule margin="s" />
      <EuiFlexGroup gutterSize="l">
        <EuiFlexItem>
          <EuiStat
            title={`${diskFmt.value} ${diskFmt.unit}`}
            description="Total Disk (all copies)"
            titleSize="s"
          >
            <EuiText size="xs" color="subdued">
              1 primary + {replicas} replica(s) = {result.totalCopies} total copies
            </EuiText>
          </EuiStat>
        </EuiFlexItem>
        <EuiFlexItem>
          <EuiStat
            title={`${ramFmt.value} ${ramFmt.unit}`}
            description="Total Off-Heap RAM (all copies)"
            titleSize="s"
          >
            <EuiText size="xs" color="subdued">
              Spread across data nodes holding these replicas
            </EuiText>
          </EuiStat>
        </EuiFlexItem>
      </EuiFlexGroup>
    </EuiPanel>
  );
}
