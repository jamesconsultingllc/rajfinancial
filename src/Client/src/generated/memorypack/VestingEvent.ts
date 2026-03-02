import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class VestingEvent {
    date: Date;
    shares: number;

    constructor() {
        this.date = new Date(0);
        this.shares = 0;

    }

    static serialize(value: VestingEvent | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: VestingEvent | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(2);
        writer.writeDate(value.date);
        writer.writeFloat64(value.shares);

    }

    static serializeArray(value: (VestingEvent | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (VestingEvent | null)[] | null): void {
        writer.writeArray(value, (writer, x) => VestingEvent.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): VestingEvent | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): VestingEvent | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new VestingEvent();
        if (count == 2) {
            value.date = reader.readDate();
            value.shares = reader.readFloat64();

        }
        else if (count > 2) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.date = reader.readDate(); if (count == 1) return value;
            value.shares = reader.readFloat64(); if (count == 2) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (VestingEvent | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (VestingEvent | null)[] | null {
        return reader.readArray(reader => VestingEvent.deserializeCore(reader));
    }
}
