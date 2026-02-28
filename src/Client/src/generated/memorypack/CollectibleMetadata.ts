import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { CollectibleCategory } from "./CollectibleCategory";
import { ItemCondition } from "./ItemCondition";
import { IAssetMetadata } from "./IAssetMetadata";

export class CollectibleMetadata implements IAssetMetadata {
    category: CollectibleCategory;
    customCategory: string | null;
    condition: number | null;
    provenance: string | null;
    serialNumber: string | null;
    certificationBody: string | null;
    certificationNumber: string | null;
    grade: string | null;
    edition: string | null;
    artist: string | null;
    appraiserName: string | null;
    lastAppraisalDate: Date | null;
    insuredValue: number | null;

    constructor() {
        this.category = 0;
        this.customCategory = null;
        this.condition = null;
        this.provenance = null;
        this.serialNumber = null;
        this.certificationBody = null;
        this.certificationNumber = null;
        this.grade = null;
        this.edition = null;
        this.artist = null;
        this.appraiserName = null;
        this.lastAppraisalDate = null;
        this.insuredValue = null;

    }

    static serialize(value: CollectibleMetadata | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: CollectibleMetadata | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(13);
        writer.writeInt32(value.category);
        writer.writeString(value.customCategory);
        writer.writeNullableInt32(value.condition);
        writer.writeString(value.provenance);
        writer.writeString(value.serialNumber);
        writer.writeString(value.certificationBody);
        writer.writeString(value.certificationNumber);
        writer.writeString(value.grade);
        writer.writeString(value.edition);
        writer.writeString(value.artist);
        writer.writeString(value.appraiserName);
        writer.writeNullableDate(value.lastAppraisalDate);
        writer.writeNullableFloat64(value.insuredValue);

    }

    static serializeArray(value: (CollectibleMetadata | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (CollectibleMetadata | null)[] | null): void {
        writer.writeArray(value, (writer, x) => CollectibleMetadata.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): CollectibleMetadata | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): CollectibleMetadata | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new CollectibleMetadata();
        if (count == 13) {
            value.category = reader.readInt32();
            value.customCategory = reader.readString();
            value.condition = reader.readNullableInt32();
            value.provenance = reader.readString();
            value.serialNumber = reader.readString();
            value.certificationBody = reader.readString();
            value.certificationNumber = reader.readString();
            value.grade = reader.readString();
            value.edition = reader.readString();
            value.artist = reader.readString();
            value.appraiserName = reader.readString();
            value.lastAppraisalDate = reader.readNullableDate();
            value.insuredValue = reader.readNullableFloat64();

        }
        else if (count > 13) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.category = reader.readInt32(); if (count == 1) return value;
            value.customCategory = reader.readString(); if (count == 2) return value;
            value.condition = reader.readNullableInt32(); if (count == 3) return value;
            value.provenance = reader.readString(); if (count == 4) return value;
            value.serialNumber = reader.readString(); if (count == 5) return value;
            value.certificationBody = reader.readString(); if (count == 6) return value;
            value.certificationNumber = reader.readString(); if (count == 7) return value;
            value.grade = reader.readString(); if (count == 8) return value;
            value.edition = reader.readString(); if (count == 9) return value;
            value.artist = reader.readString(); if (count == 10) return value;
            value.appraiserName = reader.readString(); if (count == 11) return value;
            value.lastAppraisalDate = reader.readNullableDate(); if (count == 12) return value;
            value.insuredValue = reader.readNullableFloat64(); if (count == 13) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (CollectibleMetadata | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (CollectibleMetadata | null)[] | null {
        return reader.readArray(reader => CollectibleMetadata.deserializeCore(reader));
    }
}
