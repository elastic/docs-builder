import type {
  CalculatorInputs,
  SizingResult,
  ValidationResult,
  BreakdownItem,
} from './types';

/** Return the number of raw bytes per vector based on element type + dimensions. */
function rawBytesPerVector(elementType: string, D: number): number {
  switch (elementType) {
    case 'float':
      return D * 4;
    case 'bfloat16':
      return D * 2;
    case 'byte':
      return D;
    case 'bit':
      return Math.ceil(D / 8);
    default:
      return D * 4;
  }
}

/** Human-readable byte formatting. */
export function formatBytes(bytes: number): { value: string; unit: string } {
  if (bytes === 0) return { value: '0', unit: 'bytes' };
  const units = ['bytes', 'KB', 'MB', 'GB', 'TB', 'PB'];
  let idx = 0;
  let val = bytes;
  while (val >= 1024 && idx < units.length - 1) {
    val /= 1024;
    idx++;
  }
  const formatted =
    val < 10 ? val.toFixed(2) : val < 100 ? val.toFixed(1) : val.toFixed(0);
  return { value: formatted, unit: units[idx] };
}

export function formatBytesString(bytes: number): string {
  const f = formatBytes(bytes);
  return `${f.value} ${f.unit}`;
}

/** Returns the list of available quantization options for a given element type + index type. */
export function getAvailableQuantizations(
  elementType: string,
  indexType: string
): { value: string; label: string }[] {
  if (indexType === 'disk_bbq') {
    return [{ value: 'bbq', label: 'BBQ (built-in)' }];
  }
  const options: { value: string; label: string }[] = [
    { value: 'none', label: 'None' },
  ];
  if (elementType === 'float' || elementType === 'bfloat16') {
    options.push(
      { value: 'int8', label: 'int8' },
      { value: 'int4', label: 'int4' },
      { value: 'bbq', label: 'BBQ' }
    );
  }
  return options;
}

/** Validate inputs and return any warnings. */
export function validate(inputs: CalculatorInputs): ValidationResult {
  const { numVectors, numDimensions, elementType, quantization, indexType } =
    inputs;

  if (
    isNaN(numVectors) ||
    isNaN(numDimensions) ||
    numVectors <= 0 ||
    numDimensions <= 0
  ) {
    return { valid: false };
  }

  if (numDimensions > 4096) {
    return {
      valid: false,
      warning:
        'Elasticsearch supports a maximum of 4,096 dimensions for dense_vector fields.',
      warningLink:
        'https://www.elastic.co/docs/reference/elasticsearch/mapping-reference/dense-vector#dense-vector-params',
    };
  }

  if (
    (elementType === 'byte' || elementType === 'bit') &&
    quantization !== 'none' &&
    indexType !== 'disk_bbq'
  ) {
    return {
      valid: true,
      warning: `Quantization is not applicable to ${elementType} element type.`,
    };
  }

  if (
    elementType === 'float' &&
    numDimensions >= 384 &&
    quantization === 'none' &&
    indexType !== 'disk_bbq'
  ) {
    return {
      valid: true,
      note: 'For float vectors with dimensions ≥ 384, Elastic strongly recommends using a quantized index to reduce memory footprint.',
    };
  }

  return { valid: true };
}

/** Compute all sizing estimates. */
export function calculate(inputs: CalculatorInputs): SizingResult | null {
  const {
    numVectors: V,
    numDimensions: D,
    elementType,
    indexType,
    quantization,
    replicas,
    hnswM: m,
    vectorsPerCluster: vpc,
  } = inputs;

  if (isNaN(V) || isNaN(D) || V <= 0 || D <= 0) return null;
  if (D > 4096) return null;

  const formulas: string[] = [];

  // --- Disk ---

  // Raw vectors
  const rpv = rawBytesPerVector(elementType, D);
  const rawDisk = V * rpv;
  formulas.push(
    `Raw vectors on disk = ${V.toLocaleString()} × ${rpv} = ${formatBytesString(rawDisk)}`
  );

  // Quantized vectors (disk)
  let quantDisk = 0;
  let quantLabel = '';
  if (indexType !== 'disk_bbq') {
    switch (quantization) {
      case 'int8':
        quantDisk = V * (D + 4);
        quantLabel = 'int8 quantized vectors';
        formulas.push(
          `int8 quantized = V × (D + 4) = ${formatBytesString(quantDisk)}`
        );
        break;
      case 'int4':
        quantDisk = V * (Math.ceil(D / 2) + 4);
        quantLabel = 'int4 quantized vectors';
        formulas.push(
          `int4 quantized = V × (⌈D/2⌉ + 4) = ${formatBytesString(quantDisk)}`
        );
        break;
      case 'bbq':
        quantDisk = V * (Math.ceil(D / 8) + 14);
        quantLabel = 'BBQ quantized vectors';
        formulas.push(
          `BBQ quantized = V × (⌈D/8⌉ + 14) = ${formatBytesString(quantDisk)}`
        );
        break;
    }
  }

  // Index structure (disk)
  let indexDisk = 0;
  let indexLabel = '';
  let bbqCentroids = 0;
  let bbqVectors = 0;

  if (indexType === 'hnsw') {
    indexDisk = V * 4 * m;
    indexLabel = 'HNSW graph';
    formulas.push(
      `HNSW graph = V × 4 × ${m} = ${formatBytesString(indexDisk)}`
    );
  } else if (indexType === 'disk_bbq') {
    const nc = Math.ceil(V / vpc);
    bbqCentroids = nc * D * 4 + nc * (D + 14);
    bbqVectors = V * ((Math.ceil(D / 8) + 14 + 2) * 2);
    indexDisk = bbqCentroids + bbqVectors;
    indexLabel = 'DiskBBQ structures';
    formulas.push(
      `DiskBBQ clusters = ⌈V / ${vpc}⌉ = ${nc.toLocaleString()}`
    );
    formulas.push(`Centroid bytes = ${formatBytesString(bbqCentroids)}`);
    formulas.push(
      `Quantized cluster vectors = ${formatBytesString(bbqVectors)}`
    );
  }

  const totalDisk = rawDisk + quantDisk + indexDisk;
  formulas.push(
    `Total disk (per replica) = ${formatBytesString(totalDisk)}`
  );

  // Build disk breakdown
  const diskBreakdown: BreakdownItem[] = [
    { label: 'Raw vectors', bytes: rawDisk, color: 'primary' },
  ];
  if (quantDisk > 0)
    diskBreakdown.push({
      label: quantLabel,
      bytes: quantDisk,
      color: 'accent',
    });
  if (indexDisk > 0)
    diskBreakdown.push({
      label: indexLabel,
      bytes: indexDisk,
      color: 'warning',
    });

  // --- RAM ---
  let ramVectors = 0;
  let ramVectorsLabel = '';
  let ramIndex = 0;
  let ramIndexLabel = '';

  if (indexType === 'hnsw' || indexType === 'flat') {
    switch (quantization) {
      case 'none':
        ramVectors = rawDisk;
        ramVectorsLabel = 'Raw vectors in RAM';
        formulas.push(`Vector RAM = ${formatBytesString(ramVectors)}`);
        break;
      case 'int8':
        ramVectors = V * (D + 4);
        ramVectorsLabel = 'int8 vectors in RAM';
        formulas.push(
          `Vector RAM (int8) = ${formatBytesString(ramVectors)}`
        );
        break;
      case 'int4':
        ramVectors = V * (Math.ceil(D / 2) + 4);
        ramVectorsLabel = 'int4 vectors in RAM';
        formulas.push(
          `Vector RAM (int4) = ${formatBytesString(ramVectors)}`
        );
        break;
      case 'bbq':
        ramVectors = V * (Math.ceil(D / 8) + 14);
        ramVectorsLabel = 'BBQ vectors in RAM';
        formulas.push(
          `Vector RAM (BBQ) = ${formatBytesString(ramVectors)}`
        );
        break;
    }
    if (indexType === 'hnsw') {
      ramIndex = V * 4 * m;
      ramIndexLabel = 'HNSW graph in RAM';
      formulas.push(`HNSW graph RAM = ${formatBytesString(ramIndex)}`);
    }
  } else if (indexType === 'disk_bbq') {
    const fullIndex = bbqCentroids + bbqVectors;
    ramVectors = Math.ceil(fullIndex * 0.05);
    ramVectorsLabel = 'DiskBBQ structures (~5% in RAM)';
    formulas.push(
      `DiskBBQ RAM ≈ 5% × ${formatBytesString(fullIndex)} = ${formatBytesString(ramVectors)}`
    );
    formulas.push(
      '  Note: 1–5% of index structure in RAM is typically sufficient'
    );
  }

  const totalRam = ramVectors + ramIndex;
  formulas.push(
    `Total off-heap RAM (per replica) = ${formatBytesString(totalRam)}`
  );

  // Build RAM breakdown
  const ramBreakdown: BreakdownItem[] = [];
  if (ramVectors > 0)
    ramBreakdown.push({
      label: ramVectorsLabel,
      bytes: ramVectors,
      color: 'primary',
    });
  if (ramIndex > 0)
    ramBreakdown.push({
      label: ramIndexLabel,
      bytes: ramIndex,
      color: 'accent',
    });

  // Cluster totals
  const totalCopies = 1 + replicas;
  const clusterDisk = totalDisk * totalCopies;
  const clusterRam = totalRam * totalCopies;

  if (replicas > 0) {
    formulas.push('');
    formulas.push(
      `Cluster total disk = per-replica × ${totalCopies} copies = ${formatBytesString(clusterDisk)}`
    );
    formulas.push(
      `Cluster total RAM  = per-replica × ${totalCopies} copies = ${formatBytesString(clusterRam)}`
    );
  }

  return {
    diskBreakdown,
    ramBreakdown,
    totalDisk,
    totalRam,
    clusterDisk,
    clusterRam,
    totalCopies,
    formulas,
  };
}
