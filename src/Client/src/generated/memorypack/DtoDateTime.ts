import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class DtoDateTime {
    value: Date;

    constructor() {
        this.value = new Date(0);

    }

    static serialize(value: DtoDateTime | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: DtoDateTime | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(1);
        writer.writeDate(value.value);

    }

    static serializeArray(value: (DtoDateTime | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (DtoDateTime | null)[] | null): void {
        writer.writeArray(value, (writer, x) => DtoDateTime.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): DtoDateTime | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): DtoDateTime | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new DtoDateTime();
        if (count == 1) {
            value.value = reader.readDate();

        }
        else if (count > 1) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.value = reader.readDate(); if (count == 1) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (DtoDateTime | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (DtoDateTime | null)[] | null {
        return reader.readArray(reader => DtoDateTime.deserializeCore(reader));
    }
}
