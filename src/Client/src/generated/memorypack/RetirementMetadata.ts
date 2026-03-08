import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { RetirementAccountType } from "./RetirementAccountType";
import { IAssetMetadata } from "./IAssetMetadata";
import { EmployerMatchTier } from "./EmployerMatchTier";

export class RetirementMetadata implements IAssetMetadata {
    accountType: RetirementAccountType;
    employerMatchTiers: (EmployerMatchTier | null)[] | null;
    vestingPercent: number | null;
    vestingScheduleMonths: number | null;
    projectedAnnualContribution: number | null;
    salary: number | null;

    constructor() {
        this.accountType = 0;
        this.employerMatchTiers = null;
        this.vestingPercent = null;
        this.vestingScheduleMonths = null;
        this.projectedAnnualContribution = null;
        this.salary = null;

    }

    static serialize(value: RetirementMetadata | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: RetirementMetadata | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(6);
        writer.writeInt32(value.accountType);
        writer.writeArray(value.employerMatchTiers, (writer, x) => EmployerMatchTier.serializeCore(writer, x));
        writer.writeNullableFloat64(value.vestingPercent);
        writer.writeNullableInt32(value.vestingScheduleMonths);
        writer.writeNullableFloat64(value.projectedAnnualContribution);
        writer.writeNullableFloat64(value.salary);

    }

    static serializeArray(value: (RetirementMetadata | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (RetirementMetadata | null)[] | null): void {
        writer.writeArray(value, (writer, x) => RetirementMetadata.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): RetirementMetadata | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): RetirementMetadata | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new RetirementMetadata();
        if (count == 6) {
            value.accountType = reader.readInt32();
            value.employerMatchTiers = reader.readArray(reader => EmployerMatchTier.deserializeCore(reader));
            value.vestingPercent = reader.readNullableFloat64();
            value.vestingScheduleMonths = reader.readNullableInt32();
            value.projectedAnnualContribution = reader.readNullableFloat64();
            value.salary = reader.readNullableFloat64();

        }
        else if (count > 6) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.accountType = reader.readInt32(); if (count == 1) return value;
            value.employerMatchTiers = reader.readArray(reader => EmployerMatchTier.deserializeCore(reader)); if (count == 2) return value;
            value.vestingPercent = reader.readNullableFloat64(); if (count == 3) return value;
            value.vestingScheduleMonths = reader.readNullableInt32(); if (count == 4) return value;
            value.projectedAnnualContribution = reader.readNullableFloat64(); if (count == 5) return value;
            value.salary = reader.readNullableFloat64(); if (count == 6) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (RetirementMetadata | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (RetirementMetadata | null)[] | null {
        return reader.readArray(reader => RetirementMetadata.deserializeCore(reader));
    }
}
