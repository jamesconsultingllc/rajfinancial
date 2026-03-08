// ============================================================================
// RAJ Financial – Beneficiary Allocation Validator (client-side)
// ============================================================================
// TypeScript port of Shared/Validators/AllocationValidator.cs.
// Keeps validation logic identical on client and server.
// ============================================================================

import { BeneficiaryAssignmentDto } from "../generated/memorypack/BeneficiaryAssignmentDto";

/* ------------------------------------------------------------------ */
/*  Constants                                                          */
/* ------------------------------------------------------------------ */

export const BeneficiaryType = {
  PRIMARY: "Primary",
  CONTINGENT: "Contingent",
} as const;

export const AllocationErrorCodes = {
  PRIMARY_TOTAL_INVALID: "ALLOCATION_PRIMARY_TOTAL_INVALID",
  CONTINGENT_TOTAL_INVALID: "ALLOCATION_CONTINGENT_TOTAL_INVALID",
  PERCENT_OUT_OF_RANGE: "ALLOCATION_PERCENT_OUT_OF_RANGE",
  DUPLICATE_BENEFICIARY: "ALLOCATION_DUPLICATE_BENEFICIARY",
} as const;

const MIN_PERCENT = 0.01;
const MAX_PERCENT = 100;
const REQUIRED_TOTAL = 100;
const TOLERANCE = 0.0001;

/* ------------------------------------------------------------------ */
/*  Types                                                              */
/* ------------------------------------------------------------------ */

export interface AllocationValidationError {
  errorCode: string;
  message: string;
}

export interface AllocationValidationResult {
  primaryTotal: number;
  contingentTotal: number;
  isPrimaryValid: boolean;
  isContingentValid: boolean;
  errors: AllocationValidationError[];
  isValid: boolean;
}

/* ------------------------------------------------------------------ */
/*  Validator                                                          */
/* ------------------------------------------------------------------ */

function isBeneficiaryType(
  type: string,
  expected: string,
): boolean {
  return type.localeCompare(expected, undefined, { sensitivity: "accent" }) === 0;
}

function findDuplicates(
  assignments: BeneficiaryAssignmentDto[],
  type: string,
  errors: AllocationValidationError[],
): void {
  const seen = new Map<string, string>();
  for (const a of assignments) {
    if (!isBeneficiaryType(a.type, type)) continue;
    if (seen.has(a.beneficiaryId)) {
      errors.push({
        errorCode: AllocationErrorCodes.DUPLICATE_BENEFICIARY,
        message: `'${a.beneficiaryName}' is assigned more than once as a ${type} beneficiary`,
      });
    } else {
      seen.set(a.beneficiaryId, a.beneficiaryName);
    }
  }
}

/**
 * Validates a list of beneficiary assignments against the allocation rules.
 *
 * Rules:
 * - Primary beneficiaries must total exactly 100 %.
 * - Contingent beneficiaries must total exactly 100 % (if any are assigned).
 * - Individual allocations must be between 0.01 % and 100 %.
 * - No duplicate beneficiaries per type.
 */
export function validateAllocations(
  assignments: BeneficiaryAssignmentDto[],
): AllocationValidationResult {
  const errors: AllocationValidationError[] = [];

  // Individual allocation range checks
  for (const a of assignments) {
    if (a.allocationPercent < MIN_PERCENT || a.allocationPercent > MAX_PERCENT) {
      errors.push({
        errorCode: AllocationErrorCodes.PERCENT_OUT_OF_RANGE,
        message: `Allocation for '${a.beneficiaryName}' must be between ${MIN_PERCENT}% and ${MAX_PERCENT}%`,
      });
    }
  }

  // Duplicate checks per type
  findDuplicates(assignments, BeneficiaryType.PRIMARY, errors);
  findDuplicates(assignments, BeneficiaryType.CONTINGENT, errors);

  // Primary total
  const primary = assignments.filter((a) =>
    isBeneficiaryType(a.type, BeneficiaryType.PRIMARY),
  );
  const primaryTotal = primary.reduce((sum, a) => sum + a.allocationPercent, 0);
  const isPrimaryValid = Math.abs(primaryTotal - REQUIRED_TOTAL) < TOLERANCE;

  if (!isPrimaryValid && primary.length > 0) {
    errors.push({
      errorCode: AllocationErrorCodes.PRIMARY_TOTAL_INVALID,
      message: `Primary beneficiary allocations must total exactly 100% (currently ${primaryTotal.toFixed(2)}%)`,
    });
  }

  // Contingent total
  const contingent = assignments.filter((a) =>
    isBeneficiaryType(a.type, BeneficiaryType.CONTINGENT),
  );
  const contingentTotal = contingent.reduce(
    (sum, a) => sum + a.allocationPercent,
    0,
  );
  const isContingentValid =
    contingent.length === 0 ||
    Math.abs(contingentTotal - REQUIRED_TOTAL) < TOLERANCE;

  if (!isContingentValid) {
    errors.push({
      errorCode: AllocationErrorCodes.CONTINGENT_TOTAL_INVALID,
      message: `Contingent beneficiary allocations must total exactly 100% (currently ${contingentTotal.toFixed(2)}%)`,
    });
  }

  return {
    primaryTotal,
    contingentTotal,
    isPrimaryValid,
    isContingentValid,
    errors,
    isValid: errors.length === 0,
  };
}
