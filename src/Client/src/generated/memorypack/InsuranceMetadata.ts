import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { InsurancePolicyType } from "./InsurancePolicyType";
import { PremiumFrequency } from "./PremiumFrequency";
import { DividendOption } from "./DividendOption";
import { IAssetMetadata } from "./IAssetMetadata";
import { PolicyRider } from "./PolicyRider";

export class InsuranceMetadata implements IAssetMetadata {
    policyType: InsurancePolicyType;
    cashValue: number | null;
    deathBenefit: number | null;
    premiumAmount: number | null;
    premiumFrequency: number | null;
    policyStartDate: Date | null;
    policyEndDate: Date | null;
    riders: (PolicyRider | null)[] | null;
    dividendOption: number | null;
    annualDividend: number | null;

    constructor() {
        this.policyType = 0;
        this.cashValue = null;
        this.deathBenefit = null;
        this.premiumAmount = null;
        this.premiumFrequency = null;
        this.policyStartDate = null;
        this.policyEndDate = null;
        this.riders = null;
        this.dividendOption = null;
        this.annualDividend = null;

    }

    static serialize(value: InsuranceMetadata | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: InsuranceMetadata | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(10);
        writer.writeInt32(value.policyType);
        writer.writeNullableFloat64(value.cashValue);
        writer.writeNullableFloat64(value.deathBenefit);
        writer.writeNullableFloat64(value.premiumAmount);
        writer.writeNullableInt32(value.premiumFrequency);
        writer.writeNullableDate(value.policyStartDate);
        writer.writeNullableDate(value.policyEndDate);
        writer.writeArray(value.riders, (writer, x) => PolicyRider.serializeCore(writer, x));
        writer.writeNullableInt32(value.dividendOption);
        writer.writeNullableFloat64(value.annualDividend);

    }

    static serializeArray(value: (InsuranceMetadata | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (InsuranceMetadata | null)[] | null): void {
        writer.writeArray(value, (writer, x) => InsuranceMetadata.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): InsuranceMetadata | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): InsuranceMetadata | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new InsuranceMetadata();
        if (count == 10) {
            value.policyType = reader.readInt32();
            value.cashValue = reader.readNullableFloat64();
            value.deathBenefit = reader.readNullableFloat64();
            value.premiumAmount = reader.readNullableFloat64();
            value.premiumFrequency = reader.readNullableInt32();
            value.policyStartDate = reader.readNullableDate();
            value.policyEndDate = reader.readNullableDate();
            value.riders = reader.readArray(reader => PolicyRider.deserializeCore(reader));
            value.dividendOption = reader.readNullableInt32();
            value.annualDividend = reader.readNullableFloat64();

        }
        else if (count > 10) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.policyType = reader.readInt32(); if (count == 1) return value;
            value.cashValue = reader.readNullableFloat64(); if (count == 2) return value;
            value.deathBenefit = reader.readNullableFloat64(); if (count == 3) return value;
            value.premiumAmount = reader.readNullableFloat64(); if (count == 4) return value;
            value.premiumFrequency = reader.readNullableInt32(); if (count == 5) return value;
            value.policyStartDate = reader.readNullableDate(); if (count == 6) return value;
            value.policyEndDate = reader.readNullableDate(); if (count == 7) return value;
            value.riders = reader.readArray(reader => PolicyRider.deserializeCore(reader)); if (count == 8) return value;
            value.dividendOption = reader.readNullableInt32(); if (count == 9) return value;
            value.annualDividend = reader.readNullableFloat64(); if (count == 10) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (InsuranceMetadata | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (InsuranceMetadata | null)[] | null {
        return reader.readArray(reader => InsuranceMetadata.deserializeCore(reader));
    }
}
