import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { IpType } from "./IpType";
import { IpStatus } from "./IpStatus";
import { IAssetMetadata } from "./IAssetMetadata";

export class IntellectualPropertyMetadata implements IAssetMetadata {
    ipType: IpType;
    registrationNumber: string | null;
    jurisdiction: string | null;
    filingDate: Date | null;
    issueDate: Date | null;
    expirationDate: Date | null;
    status: number | null;
    licensee: string | null;
    royaltyRate: number | null;
    annualRevenue: number | null;

    constructor() {
        this.ipType = 0;
        this.registrationNumber = null;
        this.jurisdiction = null;
        this.filingDate = null;
        this.issueDate = null;
        this.expirationDate = null;
        this.status = null;
        this.licensee = null;
        this.royaltyRate = null;
        this.annualRevenue = null;

    }

    static serialize(value: IntellectualPropertyMetadata | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: IntellectualPropertyMetadata | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(10);
        writer.writeInt32(value.ipType);
        writer.writeString(value.registrationNumber);
        writer.writeString(value.jurisdiction);
        writer.writeNullableDate(value.filingDate);
        writer.writeNullableDate(value.issueDate);
        writer.writeNullableDate(value.expirationDate);
        writer.writeNullableInt32(value.status);
        writer.writeString(value.licensee);
        writer.writeNullableFloat64(value.royaltyRate);
        writer.writeNullableFloat64(value.annualRevenue);

    }

    static serializeArray(value: (IntellectualPropertyMetadata | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (IntellectualPropertyMetadata | null)[] | null): void {
        writer.writeArray(value, (writer, x) => IntellectualPropertyMetadata.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): IntellectualPropertyMetadata | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): IntellectualPropertyMetadata | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new IntellectualPropertyMetadata();
        if (count == 10) {
            value.ipType = reader.readInt32();
            value.registrationNumber = reader.readString();
            value.jurisdiction = reader.readString();
            value.filingDate = reader.readNullableDate();
            value.issueDate = reader.readNullableDate();
            value.expirationDate = reader.readNullableDate();
            value.status = reader.readNullableInt32();
            value.licensee = reader.readString();
            value.royaltyRate = reader.readNullableFloat64();
            value.annualRevenue = reader.readNullableFloat64();

        }
        else if (count > 10) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.ipType = reader.readInt32(); if (count == 1) return value;
            value.registrationNumber = reader.readString(); if (count == 2) return value;
            value.jurisdiction = reader.readString(); if (count == 3) return value;
            value.filingDate = reader.readNullableDate(); if (count == 4) return value;
            value.issueDate = reader.readNullableDate(); if (count == 5) return value;
            value.expirationDate = reader.readNullableDate(); if (count == 6) return value;
            value.status = reader.readNullableInt32(); if (count == 7) return value;
            value.licensee = reader.readString(); if (count == 8) return value;
            value.royaltyRate = reader.readNullableFloat64(); if (count == 9) return value;
            value.annualRevenue = reader.readNullableFloat64(); if (count == 10) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (IntellectualPropertyMetadata | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (IntellectualPropertyMetadata | null)[] | null {
        return reader.readArray(reader => IntellectualPropertyMetadata.deserializeCore(reader));
    }
}
