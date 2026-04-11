import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class UpdateProfileRequest {
    displayName: string;
    locale: string;
    timezone: string;
    currency: string;

    constructor() {
        this.displayName = "";
        this.locale = "";
        this.timezone = "";
        this.currency = "";

    }

    static serialize(value: UpdateProfileRequest | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: UpdateProfileRequest | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(4);
        writer.writeString(value.displayName);
        writer.writeString(value.locale);
        writer.writeString(value.timezone);
        writer.writeString(value.currency);

    }

    static serializeArray(value: (UpdateProfileRequest | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (UpdateProfileRequest | null)[] | null): void {
        writer.writeArray(value, (writer, x) => UpdateProfileRequest.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): UpdateProfileRequest | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): UpdateProfileRequest | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new UpdateProfileRequest();
        if (count == 4) {
            value.displayName = reader.readString();
            value.locale = reader.readString();
            value.timezone = reader.readString();
            value.currency = reader.readString();

        }
        else if (count > 4) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.displayName = reader.readString(); if (count == 1) return value;
            value.locale = reader.readString(); if (count == 2) return value;
            value.timezone = reader.readString(); if (count == 3) return value;
            value.currency = reader.readString(); if (count == 4) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (UpdateProfileRequest | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (UpdateProfileRequest | null)[] | null {
        return reader.readArray(reader => UpdateProfileRequest.deserializeCore(reader));
    }
}
