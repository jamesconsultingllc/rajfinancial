import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { DtoDateTime } from "./DtoDateTime";

export class UserProfileResponse {
    userId: string;
    displayName: string;
    locale: string;
    timezone: string;
    currency: string;
    createdAt: DtoDateTime | null;

    constructor() {
        this.userId = "";
        this.displayName = "";
        this.locale = "";
        this.timezone = "";
        this.currency = "";
        this.createdAt = null;

    }

    static serialize(value: UserProfileResponse | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: UserProfileResponse | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(6);
        writer.writeString(value.userId);
        writer.writeString(value.displayName);
        writer.writeString(value.locale);
        writer.writeString(value.timezone);
        writer.writeString(value.currency);
        DtoDateTime.serializeCore(writer, value.createdAt);

    }

    static serializeArray(value: (UserProfileResponse | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (UserProfileResponse | null)[] | null): void {
        writer.writeArray(value, (writer, x) => UserProfileResponse.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): UserProfileResponse | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): UserProfileResponse | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new UserProfileResponse();
        if (count == 6) {
            value.userId = reader.readString();
            value.displayName = reader.readString();
            value.locale = reader.readString();
            value.timezone = reader.readString();
            value.currency = reader.readString();
            value.createdAt = DtoDateTime.deserializeCore(reader);

        }
        else if (count > 6) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.userId = reader.readString(); if (count == 1) return value;
            value.displayName = reader.readString(); if (count == 2) return value;
            value.locale = reader.readString(); if (count == 3) return value;
            value.timezone = reader.readString(); if (count == 4) return value;
            value.currency = reader.readString(); if (count == 5) return value;
            value.createdAt = DtoDateTime.deserializeCore(reader); if (count == 6) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (UserProfileResponse | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (UserProfileResponse | null)[] | null {
        return reader.readArray(reader => UserProfileResponse.deserializeCore(reader));
    }
}
