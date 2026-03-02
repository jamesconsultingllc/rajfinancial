import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { IAssetMetadata } from "./IAssetMetadata";
import { CustomField } from "./CustomField";

export class OtherAssetMetadata implements IAssetMetadata {
    category: string | null;
    customFields: (CustomField | null)[] | null;

    constructor() {
        this.category = null;
        this.customFields = null;

    }

    static serialize(value: OtherAssetMetadata | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: OtherAssetMetadata | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(2);
        writer.writeString(value.category);
        writer.writeArray(value.customFields, (writer, x) => CustomField.serializeCore(writer, x));

    }

    static serializeArray(value: (OtherAssetMetadata | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (OtherAssetMetadata | null)[] | null): void {
        writer.writeArray(value, (writer, x) => OtherAssetMetadata.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): OtherAssetMetadata | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): OtherAssetMetadata | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new OtherAssetMetadata();
        if (count == 2) {
            value.category = reader.readString();
            value.customFields = reader.readArray(reader => CustomField.deserializeCore(reader));

        }
        else if (count > 2) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.category = reader.readString(); if (count == 1) return value;
            value.customFields = reader.readArray(reader => CustomField.deserializeCore(reader)); if (count == 2) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (OtherAssetMetadata | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (OtherAssetMetadata | null)[] | null {
        return reader.readArray(reader => OtherAssetMetadata.deserializeCore(reader));
    }
}
