import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { BankAccountType } from "./BankAccountType";
import { IAssetMetadata } from "./IAssetMetadata";

export class BankAccountMetadata implements IAssetMetadata {
    bankAccountType: BankAccountType;
    routingNumber: string | null;
    apy: number | null;
    maturityDate: Date | null;
    term: number | null;
    isJointAccount: boolean | null;

    constructor() {
        this.bankAccountType = 0;
        this.routingNumber = null;
        this.apy = null;
        this.maturityDate = null;
        this.term = null;
        this.isJointAccount = null;

    }

    static serialize(value: BankAccountMetadata | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: BankAccountMetadata | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(6);
        writer.writeInt32(value.bankAccountType);
        writer.writeString(value.routingNumber);
        writer.writeNullableFloat64(value.apy);
        writer.writeNullableDate(value.maturityDate);
        writer.writeNullableInt32(value.term);
        writer.writeNullableBoolean(value.isJointAccount);

    }

    static serializeArray(value: (BankAccountMetadata | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (BankAccountMetadata | null)[] | null): void {
        writer.writeArray(value, (writer, x) => BankAccountMetadata.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): BankAccountMetadata | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): BankAccountMetadata | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new BankAccountMetadata();
        if (count == 6) {
            value.bankAccountType = reader.readInt32();
            value.routingNumber = reader.readString();
            value.apy = reader.readNullableFloat64();
            value.maturityDate = reader.readNullableDate();
            value.term = reader.readNullableInt32();
            value.isJointAccount = reader.readNullableBoolean();

        }
        else if (count > 6) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.bankAccountType = reader.readInt32(); if (count == 1) return value;
            value.routingNumber = reader.readString(); if (count == 2) return value;
            value.apy = reader.readNullableFloat64(); if (count == 3) return value;
            value.maturityDate = reader.readNullableDate(); if (count == 4) return value;
            value.term = reader.readNullableInt32(); if (count == 5) return value;
            value.isJointAccount = reader.readNullableBoolean(); if (count == 6) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (BankAccountMetadata | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (BankAccountMetadata | null)[] | null {
        return reader.readArray(reader => BankAccountMetadata.deserializeCore(reader));
    }
}
