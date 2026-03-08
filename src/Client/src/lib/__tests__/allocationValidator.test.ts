/**
 * Tests for the beneficiary allocation validator.
 *
 * @description Validates the client-side allocation rules: 100% totals,
 * per-assignment range, and duplicate detection.
 */
import { describe, it, expect } from "vitest";
import {
  validateAllocations,
  BeneficiaryType,
  AllocationErrorCodes,
} from "../../lib/allocationValidator";
import { BeneficiaryAssignmentDto } from "../../generated/memorypack/BeneficiaryAssignmentDto";

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */

let idCounter = 0;

function makeAssignment(
  name: string,
  percent: number,
  type: string,
  id?: string,
): BeneficiaryAssignmentDto {
  const dto = new BeneficiaryAssignmentDto();
  dto.beneficiaryId = id ?? `id-${++idCounter}`;
  dto.beneficiaryName = name;
  dto.relationship = "Other";
  dto.allocationPercent = percent;
  dto.type = type;
  return dto;
}

/* ------------------------------------------------------------------ */
/*  Valid scenarios                                                     */
/* ------------------------------------------------------------------ */

describe("validateAllocations — valid scenarios", () => {
  it("primary totaling 100% returns valid", () => {
    const result = validateAllocations([
      makeAssignment("Alice", 60, BeneficiaryType.PRIMARY),
      makeAssignment("Bob", 40, BeneficiaryType.PRIMARY),
    ]);

    expect(result.isValid).toBe(true);
    expect(result.isPrimaryValid).toBe(true);
    expect(result.primaryTotal).toBe(100);
    expect(result.errors).toHaveLength(0);
  });

  it("primary and contingent both totaling 100% returns valid", () => {
    const result = validateAllocations([
      makeAssignment("Alice", 50, BeneficiaryType.PRIMARY),
      makeAssignment("Bob", 50, BeneficiaryType.PRIMARY),
      makeAssignment("Carol", 70, BeneficiaryType.CONTINGENT),
      makeAssignment("Dave", 30, BeneficiaryType.CONTINGENT),
    ]);

    expect(result.isValid).toBe(true);
    expect(result.isPrimaryValid).toBe(true);
    expect(result.isContingentValid).toBe(true);
  });

  it("single primary at 100% returns valid", () => {
    const result = validateAllocations([
      makeAssignment("Alice", 100, BeneficiaryType.PRIMARY),
    ]);

    expect(result.isValid).toBe(true);
  });

  it("no contingent is still valid", () => {
    const result = validateAllocations([
      makeAssignment("Alice", 100, BeneficiaryType.PRIMARY),
    ]);

    expect(result.isContingentValid).toBe(true);
    expect(result.contingentTotal).toBe(0);
  });

  it("empty list returns valid", () => {
    const result = validateAllocations([]);

    expect(result.isValid).toBe(true);
    expect(result.primaryTotal).toBe(0);
    expect(result.contingentTotal).toBe(0);
  });

  it("minimum allocation of 0.01% is accepted", () => {
    const result = validateAllocations([
      makeAssignment("Alice", 0.01, BeneficiaryType.PRIMARY),
      makeAssignment("Bob", 99.99, BeneficiaryType.PRIMARY),
    ]);

    expect(result.isValid).toBe(true);
  });
});

/* ------------------------------------------------------------------ */
/*  Primary total validation                                           */
/* ------------------------------------------------------------------ */

describe("validateAllocations — primary total", () => {
  it("primary under 100% returns invalid", () => {
    const result = validateAllocations([
      makeAssignment("Alice", 30, BeneficiaryType.PRIMARY),
      makeAssignment("Bob", 30, BeneficiaryType.PRIMARY),
    ]);

    expect(result.isValid).toBe(false);
    expect(result.isPrimaryValid).toBe(false);
    expect(result.primaryTotal).toBe(60);
    expect(result.errors.some((e) => e.errorCode === AllocationErrorCodes.PRIMARY_TOTAL_INVALID)).toBe(true);
  });

  it("primary over 100% returns invalid", () => {
    const result = validateAllocations([
      makeAssignment("Alice", 60, BeneficiaryType.PRIMARY),
      makeAssignment("Bob", 60, BeneficiaryType.PRIMARY),
    ]);

    expect(result.isValid).toBe(false);
    expect(result.isPrimaryValid).toBe(false);
    expect(result.errors.some((e) => e.errorCode === AllocationErrorCodes.PRIMARY_TOTAL_INVALID)).toBe(true);
  });
});

/* ------------------------------------------------------------------ */
/*  Contingent total validation                                        */
/* ------------------------------------------------------------------ */

describe("validateAllocations — contingent total", () => {
  it("contingent under 100% returns invalid", () => {
    const result = validateAllocations([
      makeAssignment("Alice", 100, BeneficiaryType.PRIMARY),
      makeAssignment("Carol", 40, BeneficiaryType.CONTINGENT),
    ]);

    expect(result.isValid).toBe(false);
    expect(result.isContingentValid).toBe(false);
    expect(result.errors.some((e) => e.errorCode === AllocationErrorCodes.CONTINGENT_TOTAL_INVALID)).toBe(true);
  });

  it("contingent over 100% returns invalid", () => {
    const result = validateAllocations([
      makeAssignment("Alice", 100, BeneficiaryType.PRIMARY),
      makeAssignment("Carol", 60, BeneficiaryType.CONTINGENT),
      makeAssignment("Dave", 60, BeneficiaryType.CONTINGENT),
    ]);

    expect(result.isValid).toBe(false);
    expect(result.isContingentValid).toBe(false);
    expect(result.errors.some((e) => e.errorCode === AllocationErrorCodes.CONTINGENT_TOTAL_INVALID)).toBe(true);
  });
});

/* ------------------------------------------------------------------ */
/*  Allocation percent range                                           */
/* ------------------------------------------------------------------ */

describe("validateAllocations — percent range", () => {
  it.each([0, -1, 100.01, 200])("allocation of %d% returns out-of-range error", (percent) => {
    const result = validateAllocations([
      makeAssignment("Alice", percent, BeneficiaryType.PRIMARY),
    ]);

    expect(result.isValid).toBe(false);
    expect(result.errors.some((e) => e.errorCode === AllocationErrorCodes.PERCENT_OUT_OF_RANGE)).toBe(true);
  });

  it.each([0.01, 50, 100])("allocation of %d% has no range error", (percent) => {
    const result = validateAllocations([
      makeAssignment("Alice", percent, BeneficiaryType.PRIMARY),
    ]);

    expect(result.errors.some((e) => e.errorCode === AllocationErrorCodes.PERCENT_OUT_OF_RANGE)).toBe(false);
  });
});

/* ------------------------------------------------------------------ */
/*  Duplicate detection                                                */
/* ------------------------------------------------------------------ */

describe("validateAllocations — duplicates", () => {
  it("duplicate primary beneficiary returns error", () => {
    const result = validateAllocations([
      makeAssignment("Alice", 50, BeneficiaryType.PRIMARY, "same-id"),
      makeAssignment("Alice", 50, BeneficiaryType.PRIMARY, "same-id"),
    ]);

    expect(result.isValid).toBe(false);
    expect(result.errors.some((e) => e.errorCode === AllocationErrorCodes.DUPLICATE_BENEFICIARY)).toBe(true);
  });

  it("duplicate contingent beneficiary returns error", () => {
    const result = validateAllocations([
      makeAssignment("Alice", 100, BeneficiaryType.PRIMARY),
      makeAssignment("Bob", 50, BeneficiaryType.CONTINGENT, "same-id"),
      makeAssignment("Bob", 50, BeneficiaryType.CONTINGENT, "same-id"),
    ]);

    expect(result.isValid).toBe(false);
    expect(result.errors.some((e) => e.errorCode === AllocationErrorCodes.DUPLICATE_BENEFICIARY)).toBe(true);
  });

  it("same beneficiary in different types is not a duplicate", () => {
    const result = validateAllocations([
      makeAssignment("Alice", 100, BeneficiaryType.PRIMARY, "same-id"),
      makeAssignment("Alice", 100, BeneficiaryType.CONTINGENT, "same-id"),
    ]);

    expect(result.errors.some((e) => e.errorCode === AllocationErrorCodes.DUPLICATE_BENEFICIARY)).toBe(false);
  });
});

/* ------------------------------------------------------------------ */
/*  Case insensitivity                                                 */
/* ------------------------------------------------------------------ */

describe("validateAllocations — case insensitivity", () => {
  it.each(["primary", "PRIMARY", "Primary"])("type '%s' is recognized as primary", (type) => {
    const result = validateAllocations([
      makeAssignment("Alice", 100, type),
    ]);

    expect(result.isPrimaryValid).toBe(true);
    expect(result.primaryTotal).toBe(100);
  });
});

/* ------------------------------------------------------------------ */
/*  Floating-point tolerance                                           */
/* ------------------------------------------------------------------ */

describe("validateAllocations — floating-point tolerance", () => {
  it("33.33 + 33.33 + 33.34 = 100 is accepted", () => {
    const result = validateAllocations([
      makeAssignment("Alice", 33.33, BeneficiaryType.PRIMARY),
      makeAssignment("Bob", 33.33, BeneficiaryType.PRIMARY),
      makeAssignment("Carol", 33.34, BeneficiaryType.PRIMARY),
    ]);

    expect(result.isPrimaryValid).toBe(true);
  });
});

/* ------------------------------------------------------------------ */
/*  Multiple violations                                                */
/* ------------------------------------------------------------------ */

describe("validateAllocations — multiple violations", () => {
  it("returns all errors when multiple rules are broken", () => {
    const result = validateAllocations([
      makeAssignment("Alice", 0, BeneficiaryType.PRIMARY, "dup-id"),     // out of range
      makeAssignment("Alice", 50, BeneficiaryType.PRIMARY, "dup-id"),    // duplicate + total != 100
      makeAssignment("Bob", 150, BeneficiaryType.CONTINGENT),            // out of range + contingent != 100
    ]);

    expect(result.isValid).toBe(false);
    expect(result.errors.length).toBeGreaterThanOrEqual(3);
  });
});
