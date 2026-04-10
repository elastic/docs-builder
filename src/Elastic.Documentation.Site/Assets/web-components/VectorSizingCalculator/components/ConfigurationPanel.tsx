import { useEffect, useState } from 'react';
import {
  EuiPanel,
  EuiFlexGroup,
  EuiFlexItem,
  EuiFormRow,
  EuiFieldNumber,
  EuiFieldText,
  EuiSelect,
  EuiSpacer,
  EuiCallOut,
  EuiText,
  EuiTitle,
  EuiHorizontalRule,
  EuiLink,
  EuiButtonEmpty,
} from '@elastic/eui';
import type { ElementType, IndexType, Quantization, ValidationResult } from '../types';

const ELEMENT_TYPE_OPTIONS = [
  { value: 'float', text: 'float (4 bytes/dim)' },
  { value: 'bfloat16', text: 'bfloat16 (2 bytes/dim)' },
  { value: 'byte', text: 'byte (1 byte/dim)' },
  { value: 'bit', text: 'bit (1 bit/dim)' },
];

interface ConfigurationPanelProps {
  vectorsText: string;
  onVectorsChange: (value: string) => void;
  onVectorsBlur: () => void;
  numDimensions: number | string;
  onDimensionsChange: (value: number | string) => void;
  elementType: ElementType;
  onElementTypeChange: (value: ElementType) => void;
  indexType: IndexType;
  onIndexTypeChange: (value: IndexType) => void;
  indexTypeOptions: { value: string; text: string }[];
  quantization: Quantization;
  onQuantizationChange: (value: Quantization) => void;
  quantOptions: { value: string; label: string }[];
  replicas: number;
  onReplicasChange: (value: number) => void;
  hnswM: number;
  onHnswMChange: (value: number) => void;
  efConstruction: number;
  onEfConstructionChange: (value: number) => void;
  vectorsPerCluster: number;
  onVectorsPerClusterChange: (value: number) => void;
  validation: ValidationResult;
}

export function ConfigurationPanel({
  vectorsText,
  onVectorsChange,
  onVectorsBlur,
  numDimensions,
  onDimensionsChange,
  elementType,
  onElementTypeChange,
  indexType,
  onIndexTypeChange,
  indexTypeOptions,
  quantization,
  onQuantizationChange,
  quantOptions,
  replicas,
  onReplicasChange,
  hnswM,
  onHnswMChange,
  efConstruction,
  onEfConstructionChange,
  vectorsPerCluster,
  onVectorsPerClusterChange,
  validation,
}: ConfigurationPanelProps) {
  const [hnswAdvancedOpen, setHnswAdvancedOpen] = useState(false);
  const [diskBbqAdvancedOpen, setDiskBbqAdvancedOpen] = useState(false);

  useEffect(() => {
    if (indexType !== 'hnsw') {
      setHnswAdvancedOpen(false);
    }
    if (indexType !== 'disk_bbq') {
      setDiskBbqAdvancedOpen(false);
    }
  }, [indexType]);

  return (
    <EuiPanel paddingSize="l">
      <EuiTitle size="xxs">
        <h3>Configuration</h3>
      </EuiTitle>
      <EuiHorizontalRule margin="s" />

      <EuiFlexGroup gutterSize="m" wrap>
        <EuiFlexItem style={{ minWidth: 180 }}>
          <EuiFormRow label="Number of vectors">
            <EuiFieldText
              placeholder="e.g. 10,000,000"
              inputMode="numeric"
              value={vectorsText}
              onChange={(e) => onVectorsChange(e.target.value)}
              onBlur={onVectorsBlur}
            />
          </EuiFormRow>
        </EuiFlexItem>
        <EuiFlexItem style={{ minWidth: 140 }}>
          <EuiFormRow label="Dimensions">
            <EuiFieldNumber
              placeholder="e.g. 768"
              min={1}
              max={4096}
              value={numDimensions}
              onChange={(e) => {
                const v = e.target.value;
                onDimensionsChange(v === '' ? '' : Number(v));
              }}
            />
          </EuiFormRow>
        </EuiFlexItem>
        <EuiFlexItem style={{ minWidth: 180 }}>
          <EuiFormRow label="Element type">
            <EuiSelect
              options={ELEMENT_TYPE_OPTIONS}
              value={elementType}
              onChange={(e) => onElementTypeChange(e.target.value as ElementType)}
            />
          </EuiFormRow>
        </EuiFlexItem>
      </EuiFlexGroup>

      <EuiSpacer size="m" />

      <EuiFlexGroup gutterSize="m" wrap>
        <EuiFlexItem style={{ minWidth: 160 }}>
          <EuiFormRow label="Index structure">
            <EuiSelect
              options={indexTypeOptions}
              value={indexType}
              onChange={(e) => onIndexTypeChange(e.target.value as IndexType)}
            />
          </EuiFormRow>
        </EuiFlexItem>
        <EuiFlexItem style={{ minWidth: 140 }}>
          <EuiFormRow label="Quantization">
            <EuiSelect
              options={quantOptions.map((o) => ({
                value: o.value,
                text: o.label,
              }))}
              value={quantization}
              disabled={indexType === 'disk_bbq'}
              onChange={(e) => onQuantizationChange(e.target.value as Quantization)}
            />
          </EuiFormRow>
        </EuiFlexItem>
        <EuiFlexItem style={{ minWidth: 140 }}>
          <EuiFormRow
            label="Number of replicas"
            helpText="Total copies = 1 primary + replicas"
          >
            <EuiFieldNumber
              value={replicas}
              min={0}
              onChange={(e) => onReplicasChange(Number(e.target.value))}
            />
          </EuiFormRow>
        </EuiFlexItem>
      </EuiFlexGroup>

      {indexType === 'hnsw' && (
        <>
          <EuiSpacer size="m" />
          <EuiButtonEmpty
            size="s"
            iconType={hnswAdvancedOpen ? 'arrowDown' : 'arrowRight'}
            iconSide="left"
            flush="left"
            onClick={() => setHnswAdvancedOpen((v) => !v)}
          >
            Advanced HNSW parameters
          </EuiButtonEmpty>
          {hnswAdvancedOpen && (
            <>
              <EuiSpacer size="s" />
              <EuiFlexGroup gutterSize="m">
                <EuiFlexItem>
                  <EuiFormRow label="m (connections per node)" helpText="Default: 16">
                    <EuiFieldNumber
                      value={hnswM}
                      min={2}
                      max={512}
                      onChange={(e) => onHnswMChange(Number(e.target.value))}
                    />
                  </EuiFormRow>
                </EuiFlexItem>
                <EuiFlexItem>
                  <EuiFormRow
                    label="ef_construction"
                    helpText="Default: 100 — build-time quality, no sizing impact"
                  >
                    <EuiFieldNumber
                      value={efConstruction}
                      min={1}
                      onChange={(e) => onEfConstructionChange(Number(e.target.value))}
                    />
                  </EuiFormRow>
                </EuiFlexItem>
              </EuiFlexGroup>
            </>
          )}
        </>
      )}

      {indexType === 'disk_bbq' && (
        <>
          <EuiSpacer size="m" />
          <EuiButtonEmpty
            size="s"
            iconType={diskBbqAdvancedOpen ? 'arrowDown' : 'arrowRight'}
            iconSide="left"
            flush="left"
            onClick={() => setDiskBbqAdvancedOpen((v) => !v)}
          >
            Advanced DiskBBQ parameters
          </EuiButtonEmpty>
          {diskBbqAdvancedOpen && (
            <>
              <EuiSpacer size="s" />
              <EuiFormRow label="Vectors per cluster" helpText="Default: 384">
                <EuiFieldNumber
                  value={vectorsPerCluster}
                  min={1}
                  onChange={(e) => onVectorsPerClusterChange(Number(e.target.value))}
                />
              </EuiFormRow>
            </>
          )}
        </>
      )}

      {validation.warning && (
        <>
          <EuiSpacer size="m" />
          <EuiCallOut title={validation.warning} color="danger" iconType="warning" size="s">
            {validation.warningLink && (
              <EuiLink href={validation.warningLink} target="_blank">
                See documentation
              </EuiLink>
            )}
          </EuiCallOut>
        </>
      )}

      {validation.note && (
        <>
          <EuiSpacer size="m" />
          <EuiCallOut title="Recommendation" color="success" iconType="iInCircle" size="s">
            <EuiText size="s">
              <p>{validation.note}</p>
            </EuiText>
          </EuiCallOut>
        </>
      )}
    </EuiPanel>
  );
}
