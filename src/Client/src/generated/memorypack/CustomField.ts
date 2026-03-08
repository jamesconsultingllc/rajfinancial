import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class CustomField {
    key: string;
    value: string;

    constructor() {
        this.key = "";
        this.value = "";

    }

    static serialize(value: CustomField | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: CustomField | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(2);
        writer.writeString(value.key);
        writer.writeString(value.value);

    }

    static serializeArray(value: (CustomField | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (CustomField | null)[] | null): void {
        writer.writeArray(value, (writer, x) => CustomField.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): CustomField | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): CustomField | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new CustomField();
        if (count == 2) {
            value.key = reader.readString();
            value.value = reader.readString();

        }
        else if (count > 2) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.key = reader.readString(); if (count == 1) return value;
            value.value = reader.readString(); if (count == 2) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (CustomField | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (CustomField | null)[] | null {
        return reader.readArray(reader => CustomField.deserializeCore(reader));
    }
}
