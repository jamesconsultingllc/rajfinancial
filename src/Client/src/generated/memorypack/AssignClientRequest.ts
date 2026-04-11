import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class AssignClientRequest {
    clientEmail: string;
    accessType: string;
    categories: string[] | null;
    relationshipLabel: string | null;

    constructor() {
        this.clientEmail = "";
        this.accessType = "";
        this.categories = null;
        this.relationshipLabel = null;

    }

    static serialize(value: AssignClientRequest | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: AssignClientRequest | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(4);
        writer.writeString(value.clientEmail);
        writer.writeString(value.accessType);
        writer.writeArray(value.categories, (writer, x) => writer.writeString(x));
        writer.writeString(value.relationshipLabel);

    }

    static serializeArray(value: (AssignClientRequest | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (AssignClientRequest | null)[] | null): void {
        writer.writeArray(value, (writer, x) => AssignClientRequest.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): AssignClientRequest | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): AssignClientRequest | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new AssignClientRequest();
        if (count == 4) {
            value.clientEmail = reader.readString();
            value.accessType = reader.readString();
            value.categories = reader.readArray(reader => reader.readString());
            value.relationshipLabel = reader.readString();

        }
        else if (count > 4) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.clientEmail = reader.readString(); if (count == 1) return value;
            value.accessType = reader.readString(); if (count == 2) return value;
            value.categories = reader.readArray(reader => reader.readString()); if (count == 3) return value;
            value.relationshipLabel = reader.readString(); if (count == 4) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (AssignClientRequest | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (AssignClientRequest | null)[] | null {
        return reader.readArray(reader => AssignClientRequest.deserializeCore(reader));
    }
}
