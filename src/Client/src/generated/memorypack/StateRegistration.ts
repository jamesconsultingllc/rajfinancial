import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class StateRegistration {
    state: string;
    sosNumber: string | null;
    filingDate: Date | null;
    isFormationState: boolean;

    constructor() {
        this.state = "";
        this.sosNumber = null;
        this.filingDate = null;
        this.isFormationState = false;

    }

    static serialize(value: StateRegistration | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: StateRegistration | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(4);
        writer.writeString(value.state);
        writer.writeString(value.sosNumber);
        writer.writeNullableDate(value.filingDate);
        writer.writeBoolean(value.isFormationState);

    }

    static serializeArray(value: (StateRegistration | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (StateRegistration | null)[] | null): void {
        writer.writeArray(value, (writer, x) => StateRegistration.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): StateRegistration | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): StateRegistration | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new StateRegistration();
        if (count == 4) {
            value.state = reader.readString();
            value.sosNumber = reader.readString();
            value.filingDate = reader.readNullableDate();
            value.isFormationState = reader.readBoolean();

        }
        else if (count > 4) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.state = reader.readString(); if (count == 1) return value;
            value.sosNumber = reader.readString(); if (count == 2) return value;
            value.filingDate = reader.readNullableDate(); if (count == 3) return value;
            value.isFormationState = reader.readBoolean(); if (count == 4) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (StateRegistration | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (StateRegistration | null)[] | null {
        return reader.readArray(reader => StateRegistration.deserializeCore(reader));
    }
}
