import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { PersonalPropertyCategory } from "./PersonalPropertyCategory";
import { ItemCondition } from "./ItemCondition";
import { IAssetMetadata } from "./IAssetMetadata";

export class PersonalPropertyMetadata implements IAssetMetadata {
    category: PersonalPropertyCategory;
    customCategory: string | null;
    condition: number | null;
    serialNumber: string | null;
    brand: string | null;
    modelNumber: string | null;
    appraiserName: string | null;
    lastAppraisalDate: Date | null;
    insuredValue: number | null;

    constructor() {
        this.category = 0;
        this.customCategory = null;
        this.condition = null;
        this.serialNumber = null;
        this.brand = null;
        this.modelNumber = null;
        this.appraiserName = null;
        this.lastAppraisalDate = null;
        this.insuredValue = null;

    }

    static serialize(value: PersonalPropertyMetadata | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: PersonalPropertyMetadata | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(9);
        writer.writeInt32(value.category);
        writer.writeString(value.customCategory);
        writer.writeNullableInt32(value.condition);
        writer.writeString(value.serialNumber);
        writer.writeString(value.brand);
        writer.writeString(value.modelNumber);
        writer.writeString(value.appraiserName);
        writer.writeNullableDate(value.lastAppraisalDate);
        writer.writeNullableFloat64(value.insuredValue);

    }

    static serializeArray(value: (PersonalPropertyMetadata | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (PersonalPropertyMetadata | null)[] | null): void {
        writer.writeArray(value, (writer, x) => PersonalPropertyMetadata.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): PersonalPropertyMetadata | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): PersonalPropertyMetadata | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new PersonalPropertyMetadata();
        if (count == 9) {
            value.category = reader.readInt32();
            value.customCategory = reader.readString();
            value.condition = reader.readNullableInt32();
            value.serialNumber = reader.readString();
            value.brand = reader.readString();
            value.modelNumber = reader.readString();
            value.appraiserName = reader.readString();
            value.lastAppraisalDate = reader.readNullableDate();
            value.insuredValue = reader.readNullableFloat64();

        }
        else if (count > 9) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.category = reader.readInt32(); if (count == 1) return value;
            value.customCategory = reader.readString(); if (count == 2) return value;
            value.condition = reader.readNullableInt32(); if (count == 3) return value;
            value.serialNumber = reader.readString(); if (count == 4) return value;
            value.brand = reader.readString(); if (count == 5) return value;
            value.modelNumber = reader.readString(); if (count == 6) return value;
            value.appraiserName = reader.readString(); if (count == 7) return value;
            value.lastAppraisalDate = reader.readNullableDate(); if (count == 8) return value;
            value.insuredValue = reader.readNullableFloat64(); if (count == 9) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (PersonalPropertyMetadata | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (PersonalPropertyMetadata | null)[] | null {
        return reader.readArray(reader => PersonalPropertyMetadata.deserializeCore(reader));
    }
}
