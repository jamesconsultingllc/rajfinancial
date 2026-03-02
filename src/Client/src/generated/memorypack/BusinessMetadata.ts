import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { BusinessEntityType } from "./BusinessEntityType";
import { IAssetMetadata } from "./IAssetMetadata";
import { StateRegistration } from "./StateRegistration";

export class BusinessMetadata implements IAssetMetadata {
    entityType: BusinessEntityType;
    ownershipPercent: number;
    ein: string | null;
    naicsCode: string | null;
    dunsNumber: string | null;
    industry: string | null;
    annualRevenue: number | null;
    numberOfEmployees: number | null;
    foundedDate: Date | null;
    registrations: (StateRegistration | null)[] | null;

    constructor() {
        this.entityType = 0;
        this.ownershipPercent = 0;
        this.ein = null;
        this.naicsCode = null;
        this.dunsNumber = null;
        this.industry = null;
        this.annualRevenue = null;
        this.numberOfEmployees = null;
        this.foundedDate = null;
        this.registrations = null;

    }

    static serialize(value: BusinessMetadata | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: BusinessMetadata | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(10);
        writer.writeInt32(value.entityType);
        writer.writeFloat64(value.ownershipPercent);
        writer.writeString(value.ein);
        writer.writeString(value.naicsCode);
        writer.writeString(value.dunsNumber);
        writer.writeString(value.industry);
        writer.writeNullableFloat64(value.annualRevenue);
        writer.writeNullableInt32(value.numberOfEmployees);
        writer.writeNullableDate(value.foundedDate);
        writer.writeArray(value.registrations, (writer, x) => StateRegistration.serializeCore(writer, x));

    }

    static serializeArray(value: (BusinessMetadata | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (BusinessMetadata | null)[] | null): void {
        writer.writeArray(value, (writer, x) => BusinessMetadata.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): BusinessMetadata | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): BusinessMetadata | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new BusinessMetadata();
        if (count == 10) {
            value.entityType = reader.readInt32();
            value.ownershipPercent = reader.readFloat64();
            value.ein = reader.readString();
            value.naicsCode = reader.readString();
            value.dunsNumber = reader.readString();
            value.industry = reader.readString();
            value.annualRevenue = reader.readNullableFloat64();
            value.numberOfEmployees = reader.readNullableInt32();
            value.foundedDate = reader.readNullableDate();
            value.registrations = reader.readArray(reader => StateRegistration.deserializeCore(reader));

        }
        else if (count > 10) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.entityType = reader.readInt32(); if (count == 1) return value;
            value.ownershipPercent = reader.readFloat64(); if (count == 2) return value;
            value.ein = reader.readString(); if (count == 3) return value;
            value.naicsCode = reader.readString(); if (count == 4) return value;
            value.dunsNumber = reader.readString(); if (count == 5) return value;
            value.industry = reader.readString(); if (count == 6) return value;
            value.annualRevenue = reader.readNullableFloat64(); if (count == 7) return value;
            value.numberOfEmployees = reader.readNullableInt32(); if (count == 8) return value;
            value.foundedDate = reader.readNullableDate(); if (count == 9) return value;
            value.registrations = reader.readArray(reader => StateRegistration.deserializeCore(reader)); if (count == 10) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (BusinessMetadata | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (BusinessMetadata | null)[] | null {
        return reader.readArray(reader => BusinessMetadata.deserializeCore(reader));
    }
}
