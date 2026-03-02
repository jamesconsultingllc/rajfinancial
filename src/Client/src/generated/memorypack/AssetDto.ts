import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { AssetType } from "./AssetType";
import { IAssetMetadata } from "./IAssetMetadata";

export class AssetDto {
    id: string;
    name: string;
    type: AssetType;
    currentValue: number;
    purchasePrice: number | null;
    purchaseDate: Date | null;
    description: string | null;
    location: string | null;
    accountNumber: string | null;
    institutionName: string | null;
    isDepreciable: boolean;
    isDisposed: boolean;
    hasBeneficiaries: boolean;
    createdAt: Date;
    updatedAt: Date | null;
    metadata: IAssetMetadata | null;

    constructor() {
        this.id = "00000000-0000-0000-0000-000000000000";
        this.name = "";
        this.type = 0;
        this.currentValue = 0;
        this.purchasePrice = null;
        this.purchaseDate = null;
        this.description = null;
        this.location = null;
        this.accountNumber = null;
        this.institutionName = null;
        this.isDepreciable = false;
        this.isDisposed = false;
        this.hasBeneficiaries = false;
        this.createdAt = new Date(0);
        this.updatedAt = null;
        this.metadata = null;

    }

    static serialize(value: AssetDto | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: AssetDto | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(16);
        writer.writeGuid(value.id);
        writer.writeString(value.name);
        writer.writeInt32(value.type);
        writer.writeFloat64(value.currentValue);
        writer.writeNullableFloat64(value.purchasePrice);
        writer.writeNullableDate(value.purchaseDate);
        writer.writeString(value.description);
        writer.writeString(value.location);
        writer.writeString(value.accountNumber);
        writer.writeString(value.institutionName);
        writer.writeBoolean(value.isDepreciable);
        writer.writeBoolean(value.isDisposed);
        writer.writeBoolean(value.hasBeneficiaries);
        writer.writeDate(value.createdAt);
        writer.writeNullableDate(value.updatedAt);
        IAssetMetadata.serializeCore(writer, value.metadata);

    }

    static serializeArray(value: (AssetDto | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (AssetDto | null)[] | null): void {
        writer.writeArray(value, (writer, x) => AssetDto.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): AssetDto | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): AssetDto | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new AssetDto();
        if (count == 16) {
            value.id = reader.readGuid();
            value.name = reader.readString();
            value.type = reader.readInt32();
            value.currentValue = reader.readFloat64();
            value.purchasePrice = reader.readNullableFloat64();
            value.purchaseDate = reader.readNullableDate();
            value.description = reader.readString();
            value.location = reader.readString();
            value.accountNumber = reader.readString();
            value.institutionName = reader.readString();
            value.isDepreciable = reader.readBoolean();
            value.isDisposed = reader.readBoolean();
            value.hasBeneficiaries = reader.readBoolean();
            value.createdAt = reader.readDate();
            value.updatedAt = reader.readNullableDate();
            value.metadata = IAssetMetadata.deserializeCore(reader);

        }
        else if (count > 16) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.id = reader.readGuid(); if (count == 1) return value;
            value.name = reader.readString(); if (count == 2) return value;
            value.type = reader.readInt32(); if (count == 3) return value;
            value.currentValue = reader.readFloat64(); if (count == 4) return value;
            value.purchasePrice = reader.readNullableFloat64(); if (count == 5) return value;
            value.purchaseDate = reader.readNullableDate(); if (count == 6) return value;
            value.description = reader.readString(); if (count == 7) return value;
            value.location = reader.readString(); if (count == 8) return value;
            value.accountNumber = reader.readString(); if (count == 9) return value;
            value.institutionName = reader.readString(); if (count == 10) return value;
            value.isDepreciable = reader.readBoolean(); if (count == 11) return value;
            value.isDisposed = reader.readBoolean(); if (count == 12) return value;
            value.hasBeneficiaries = reader.readBoolean(); if (count == 13) return value;
            value.createdAt = reader.readDate(); if (count == 14) return value;
            value.updatedAt = reader.readNullableDate(); if (count == 15) return value;
            value.metadata = IAssetMetadata.deserializeCore(reader); if (count == 16) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (AssetDto | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (AssetDto | null)[] | null {
        return reader.readArray(reader => AssetDto.deserializeCore(reader));
    }
}
