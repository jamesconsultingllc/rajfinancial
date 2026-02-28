import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { PolicyRiderType } from "./PolicyRiderType";

export class PolicyRider {
    riderType: PolicyRiderType;
    name: string | null;
    value: number | null;
    annualCost: number | null;

    constructor() {
        this.riderType = 0;
        this.name = null;
        this.value = null;
        this.annualCost = null;

    }

    static serialize(value: PolicyRider | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: PolicyRider | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(4);
        writer.writeInt32(value.riderType);
        writer.writeString(value.name);
        writer.writeNullableFloat64(value.value);
        writer.writeNullableFloat64(value.annualCost);

    }

    static serializeArray(value: (PolicyRider | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (PolicyRider | null)[] | null): void {
        writer.writeArray(value, (writer, x) => PolicyRider.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): PolicyRider | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): PolicyRider | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new PolicyRider();
        if (count == 4) {
            value.riderType = reader.readInt32();
            value.name = reader.readString();
            value.value = reader.readNullableFloat64();
            value.annualCost = reader.readNullableFloat64();

        }
        else if (count > 4) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.riderType = reader.readInt32(); if (count == 1) return value;
            value.name = reader.readString(); if (count == 2) return value;
            value.value = reader.readNullableFloat64(); if (count == 3) return value;
            value.annualCost = reader.readNullableFloat64(); if (count == 4) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (PolicyRider | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (PolicyRider | null)[] | null {
        return reader.readArray(reader => PolicyRider.deserializeCore(reader));
    }
}
