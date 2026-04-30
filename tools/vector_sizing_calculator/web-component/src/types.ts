export type ElementType = 'float' | 'bfloat16' | 'byte' | 'bit';
export type IndexType = 'hnsw' | 'flat' | 'disk_bbq';
export type Quantization = 'none' | 'int8' | 'int4' | 'bbq';

export interface CalculatorInputs {
  numVectors: number;
  numDimensions: number;
  elementType: ElementType;
  indexType: IndexType;
  quantization: Quantization;
  replicas: number;
  hnswM: number;
  efConstruction: number;
  vectorsPerCluster: number;
}

export interface BreakdownItem {
  label: string;
  bytes: number;
  color: 'primary' | 'accent' | 'warning';
}

export interface SizingResult {
  diskBreakdown: BreakdownItem[];
  ramBreakdown: BreakdownItem[];
  totalDisk: number;
  totalRam: number;
  clusterDisk: number;
  clusterRam: number;
  totalCopies: number;
  formulas: string[];
}

export interface ValidationResult {
  valid: boolean;
  warning?: string;
  warningLink?: string;
  note?: string;
}
