import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class ClientAssignmentResponse {
    grantId: string;
    clientEmail: string;
    accessType: string;
    categories: string[] | null;
    relationshipLabel: string | null;
    status: string;
    createdAt: Date;

    constructor() {
        this.grantId = "00000000-0000-0000-0000-000000000000";
        this.clientEmail = "";
        this.accessType = "";
        this.categories = null;
        this.relationshipLabel = null;
        this.status = "";
        this.createdAt = new Date(0);

    }

    static serialize(value: ClientAssignmentResponse | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: ClientAssignmentResponse | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(7);
        writer.writeGuid(value.grantId);
        writer.writeString(value.clientEmail);
        writer.writeString(value.accessType);
        writer.writeArray(value.categories, (writer, x) => writer.writeString(x));
        writer.writeString(value.relationshipLabel);
        writer.writeString(value.status);
        writer.writeDate(value.createdAt);

    }

    static serializeArray(value: (ClientAssignmentResponse | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (ClientAssignmentResponse | null)[] | null): void {
        writer.writeArray(value, (writer, x) => ClientAssignmentResponse.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): ClientAssignmentResponse | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): ClientAssignmentResponse | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new ClientAssignmentResponse();
        if (count == 7) {
            value.grantId = reader.readGuid();
            value.clientEmail = reader.readString();
            value.accessType = reader.readString();
            value.categories = reader.readArray(reader => reader.readString());
            value.relationshipLabel = reader.readString();
            value.status = reader.readString();
            value.createdAt = reader.readDate();

        }
        else if (count > 7) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.grantId = reader.readGuid(); if (count == 1) return value;
            value.clientEmail = reader.readString(); if (count == 2) return value;
            value.accessType = reader.readString(); if (count == 3) return value;
            value.categories = reader.readArray(reader => reader.readString()); if (count == 4) return value;
            value.relationshipLabel = reader.readString(); if (count == 5) return value;
            value.status = reader.readString(); if (count == 6) return value;
            value.createdAt = reader.readDate(); if (count == 7) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (ClientAssignmentResponse | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (ClientAssignmentResponse | null)[] | null {
        return reader.readArray(reader => ClientAssignmentResponse.deserializeCore(reader));
    }
}
