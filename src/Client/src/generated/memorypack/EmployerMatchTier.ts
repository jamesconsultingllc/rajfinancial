import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class EmployerMatchTier {
    matchPercent: number;
    onFirst: number;

    constructor() {
        this.matchPercent = 0;
        this.onFirst = 0;

    }

    static serialize(value: EmployerMatchTier | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: EmployerMatchTier | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(2);
        writer.writeFloat64(value.matchPercent);
        writer.writeFloat64(value.onFirst);

    }

    static serializeArray(value: (EmployerMatchTier | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (EmployerMatchTier | null)[] | null): void {
        writer.writeArray(value, (writer, x) => EmployerMatchTier.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): EmployerMatchTier | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): EmployerMatchTier | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new EmployerMatchTier();
        if (count == 2) {
            value.matchPercent = reader.readFloat64();
            value.onFirst = reader.readFloat64();

        }
        else if (count > 2) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.matchPercent = reader.readFloat64(); if (count == 1) return value;
            value.onFirst = reader.readFloat64(); if (count == 2) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (EmployerMatchTier | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (EmployerMatchTier | null)[] | null {
        return reader.readArray(reader => EmployerMatchTier.deserializeCore(reader));
    }
}
