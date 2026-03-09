import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class UserProfileResponse {
    userId: string;
    email: string;
    displayName: string;
    role: string;
    isProfileComplete: boolean;
    isAdministrator: boolean;

    constructor() {
        this.userId = "";
        this.email = "";
        this.displayName = "";
        this.role = "";
        this.isProfileComplete = false;
        this.isAdministrator = false;

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
        writer.writeString(value.email);
        writer.writeString(value.displayName);
        writer.writeString(value.role);
        writer.writeBoolean(value.isProfileComplete);
        writer.writeBoolean(value.isAdministrator);

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
            value.email = reader.readString();
            value.displayName = reader.readString();
            value.role = reader.readString();
            value.isProfileComplete = reader.readBoolean();
            value.isAdministrator = reader.readBoolean();

        }
        else if (count > 6) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.userId = reader.readString(); if (count == 1) return value;
            value.email = reader.readString(); if (count == 2) return value;
            value.displayName = reader.readString(); if (count == 3) return value;
            value.role = reader.readString(); if (count == 4) return value;
            value.isProfileComplete = reader.readBoolean(); if (count == 5) return value;
            value.isAdministrator = reader.readBoolean(); if (count == 6) return value;

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
