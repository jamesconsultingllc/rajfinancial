import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { AssetType } from "./AssetType";
import { DepreciationMethod } from "./DepreciationMethod";
import { IAssetMetadata } from "./IAssetMetadata";

export class UpdateAssetRequest {
    name: string;
    type: AssetType;
    currentValue: number;
    purchasePrice: number | null;
    purchaseDate: Date | null;
    description: string | null;
    location: string | null;
    accountNumber: string | null;
    institutionName: string | null;
    depreciationMethod: number | null;
    salvageValue: number | null;
    usefulLifeMonths: number | null;
    inServiceDate: Date | null;
    marketValue: number | null;
    lastValuationDate: Date | null;
    metadata: IAssetMetadata | null;

    constructor() {
        this.name = "";
        this.type = 0;
        this.currentValue = 0;
        this.purchasePrice = null;
        this.purchaseDate = null;
        this.description = null;
        this.location = null;
        this.accountNumber = null;
        this.institutionName = null;
        this.depreciationMethod = null;
        this.salvageValue = null;
        this.usefulLifeMonths = null;
        this.inServiceDate = null;
        this.marketValue = null;
        this.lastValuationDate = null;
        this.metadata = null;

    }

    static serialize(value: UpdateAssetRequest | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: UpdateAssetRequest | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(16);
        writer.writeString(value.name);
        writer.writeInt32(value.type);
        writer.writeFloat64(value.currentValue);
        writer.writeNullableFloat64(value.purchasePrice);
        writer.writeNullableDate(value.purchaseDate);
        writer.writeString(value.description);
        writer.writeString(value.location);
        writer.writeString(value.accountNumber);
        writer.writeString(value.institutionName);
        writer.writeNullableInt32(value.depreciationMethod);
        writer.writeNullableFloat64(value.salvageValue);
        writer.writeNullableInt32(value.usefulLifeMonths);
        writer.writeNullableDate(value.inServiceDate);
        writer.writeNullableFloat64(value.marketValue);
        writer.writeNullableDate(value.lastValuationDate);
        IAssetMetadata.serializeCore(writer, value.metadata);

    }

    static serializeArray(value: (UpdateAssetRequest | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (UpdateAssetRequest | null)[] | null): void {
        writer.writeArray(value, (writer, x) => UpdateAssetRequest.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): UpdateAssetRequest | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): UpdateAssetRequest | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new UpdateAssetRequest();
        if (count == 16) {
            value.name = reader.readString();
            value.type = reader.readInt32();
            value.currentValue = reader.readFloat64();
            value.purchasePrice = reader.readNullableFloat64();
            value.purchaseDate = reader.readNullableDate();
            value.description = reader.readString();
            value.location = reader.readString();
            value.accountNumber = reader.readString();
            value.institutionName = reader.readString();
            value.depreciationMethod = reader.readNullableInt32();
            value.salvageValue = reader.readNullableFloat64();
            value.usefulLifeMonths = reader.readNullableInt32();
            value.inServiceDate = reader.readNullableDate();
            value.marketValue = reader.readNullableFloat64();
            value.lastValuationDate = reader.readNullableDate();
            value.metadata = IAssetMetadata.deserializeCore(reader);

        }
        else if (count > 16) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.name = reader.readString(); if (count == 1) return value;
            value.type = reader.readInt32(); if (count == 2) return value;
            value.currentValue = reader.readFloat64(); if (count == 3) return value;
            value.purchasePrice = reader.readNullableFloat64(); if (count == 4) return value;
            value.purchaseDate = reader.readNullableDate(); if (count == 5) return value;
            value.description = reader.readString(); if (count == 6) return value;
            value.location = reader.readString(); if (count == 7) return value;
            value.accountNumber = reader.readString(); if (count == 8) return value;
            value.institutionName = reader.readString(); if (count == 9) return value;
            value.depreciationMethod = reader.readNullableInt32(); if (count == 10) return value;
            value.salvageValue = reader.readNullableFloat64(); if (count == 11) return value;
            value.usefulLifeMonths = reader.readNullableInt32(); if (count == 12) return value;
            value.inServiceDate = reader.readNullableDate(); if (count == 13) return value;
            value.marketValue = reader.readNullableFloat64(); if (count == 14) return value;
            value.lastValuationDate = reader.readNullableDate(); if (count == 15) return value;
            value.metadata = IAssetMetadata.deserializeCore(reader); if (count == 16) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (UpdateAssetRequest | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (UpdateAssetRequest | null)[] | null {
        return reader.readArray(reader => UpdateAssetRequest.deserializeCore(reader));
    }
}
