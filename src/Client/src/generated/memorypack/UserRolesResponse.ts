import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class UserRolesResponse {
    roles: string[] | null;
    isAdministrator: boolean;

    constructor() {
        this.roles = null;
        this.isAdministrator = false;

    }

    static serialize(value: UserRolesResponse | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: UserRolesResponse | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(2);
        writer.writeArray(value.roles, (writer, x) => writer.writeString(x));
        writer.writeBoolean(value.isAdministrator);

    }

    static serializeArray(value: (UserRolesResponse | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (UserRolesResponse | null)[] | null): void {
        writer.writeArray(value, (writer, x) => UserRolesResponse.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): UserRolesResponse | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): UserRolesResponse | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new UserRolesResponse();
        if (count == 2) {
            value.roles = reader.readArray(reader => reader.readString());
            value.isAdministrator = reader.readBoolean();

        }
        else if (count > 2) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.roles = reader.readArray(reader => reader.readString()); if (count == 1) return value;
            value.isAdministrator = reader.readBoolean(); if (count == 2) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (UserRolesResponse | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (UserRolesResponse | null)[] | null {
        return reader.readArray(reader => UserRolesResponse.deserializeCore(reader));
    }
}
