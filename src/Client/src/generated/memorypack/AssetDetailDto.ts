import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { AssetType } from "./AssetType";
import { DepreciationMethod } from "./DepreciationMethod";
import { BeneficiaryAssignmentDto } from "./BeneficiaryAssignmentDto";
import { IAssetMetadata } from "./IAssetMetadata";

export class AssetDetailDto {
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
    depreciationMethod: number | null;
    salvageValue: number | null;
    usefulLifeMonths: number | null;
    inServiceDate: Date | null;
    accumulatedDepreciation: number | null;
    bookValue: number | null;
    monthlyDepreciation: number | null;
    depreciationPercentComplete: number | null;
    isDisposed: boolean;
    disposalDate: Date | null;
    disposalPrice: number | null;
    disposalNotes: string | null;
    marketValue: number | null;
    lastValuationDate: Date | null;
    hasBeneficiaries: boolean;
    beneficiaries: (BeneficiaryAssignmentDto | null)[] | null;
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
        this.depreciationMethod = null;
        this.salvageValue = null;
        this.usefulLifeMonths = null;
        this.inServiceDate = null;
        this.accumulatedDepreciation = null;
        this.bookValue = null;
        this.monthlyDepreciation = null;
        this.depreciationPercentComplete = null;
        this.isDisposed = false;
        this.disposalDate = null;
        this.disposalPrice = null;
        this.disposalNotes = null;
        this.marketValue = null;
        this.lastValuationDate = null;
        this.hasBeneficiaries = false;
        this.beneficiaries = null;
        this.createdAt = new Date(0);
        this.updatedAt = null;
        this.metadata = null;

    }

    static serialize(value: AssetDetailDto | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: AssetDetailDto | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(30);
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
        writer.writeNullableInt32(value.depreciationMethod);
        writer.writeNullableFloat64(value.salvageValue);
        writer.writeNullableInt32(value.usefulLifeMonths);
        writer.writeNullableDate(value.inServiceDate);
        writer.writeNullableFloat64(value.accumulatedDepreciation);
        writer.writeNullableFloat64(value.bookValue);
        writer.writeNullableFloat64(value.monthlyDepreciation);
        writer.writeNullableFloat64(value.depreciationPercentComplete);
        writer.writeBoolean(value.isDisposed);
        writer.writeNullableDate(value.disposalDate);
        writer.writeNullableFloat64(value.disposalPrice);
        writer.writeString(value.disposalNotes);
        writer.writeNullableFloat64(value.marketValue);
        writer.writeNullableDate(value.lastValuationDate);
        writer.writeBoolean(value.hasBeneficiaries);
        writer.writeArray(value.beneficiaries, (writer, x) => BeneficiaryAssignmentDto.serializeCore(writer, x));
        writer.writeDate(value.createdAt);
        writer.writeNullableDate(value.updatedAt);
        IAssetMetadata.serializeCore(writer, value.metadata);

    }

    static serializeArray(value: (AssetDetailDto | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (AssetDetailDto | null)[] | null): void {
        writer.writeArray(value, (writer, x) => AssetDetailDto.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): AssetDetailDto | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): AssetDetailDto | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new AssetDetailDto();
        if (count == 30) {
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
            value.depreciationMethod = reader.readNullableInt32();
            value.salvageValue = reader.readNullableFloat64();
            value.usefulLifeMonths = reader.readNullableInt32();
            value.inServiceDate = reader.readNullableDate();
            value.accumulatedDepreciation = reader.readNullableFloat64();
            value.bookValue = reader.readNullableFloat64();
            value.monthlyDepreciation = reader.readNullableFloat64();
            value.depreciationPercentComplete = reader.readNullableFloat64();
            value.isDisposed = reader.readBoolean();
            value.disposalDate = reader.readNullableDate();
            value.disposalPrice = reader.readNullableFloat64();
            value.disposalNotes = reader.readString();
            value.marketValue = reader.readNullableFloat64();
            value.lastValuationDate = reader.readNullableDate();
            value.hasBeneficiaries = reader.readBoolean();
            value.beneficiaries = reader.readArray(reader => BeneficiaryAssignmentDto.deserializeCore(reader));
            value.createdAt = reader.readDate();
            value.updatedAt = reader.readNullableDate();
            value.metadata = IAssetMetadata.deserializeCore(reader);

        }
        else if (count > 30) {
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
            value.depreciationMethod = reader.readNullableInt32(); if (count == 12) return value;
            value.salvageValue = reader.readNullableFloat64(); if (count == 13) return value;
            value.usefulLifeMonths = reader.readNullableInt32(); if (count == 14) return value;
            value.inServiceDate = reader.readNullableDate(); if (count == 15) return value;
            value.accumulatedDepreciation = reader.readNullableFloat64(); if (count == 16) return value;
            value.bookValue = reader.readNullableFloat64(); if (count == 17) return value;
            value.monthlyDepreciation = reader.readNullableFloat64(); if (count == 18) return value;
            value.depreciationPercentComplete = reader.readNullableFloat64(); if (count == 19) return value;
            value.isDisposed = reader.readBoolean(); if (count == 20) return value;
            value.disposalDate = reader.readNullableDate(); if (count == 21) return value;
            value.disposalPrice = reader.readNullableFloat64(); if (count == 22) return value;
            value.disposalNotes = reader.readString(); if (count == 23) return value;
            value.marketValue = reader.readNullableFloat64(); if (count == 24) return value;
            value.lastValuationDate = reader.readNullableDate(); if (count == 25) return value;
            value.hasBeneficiaries = reader.readBoolean(); if (count == 26) return value;
            value.beneficiaries = reader.readArray(reader => BeneficiaryAssignmentDto.deserializeCore(reader)); if (count == 27) return value;
            value.createdAt = reader.readDate(); if (count == 28) return value;
            value.updatedAt = reader.readNullableDate(); if (count == 29) return value;
            value.metadata = IAssetMetadata.deserializeCore(reader); if (count == 30) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (AssetDetailDto | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (AssetDetailDto | null)[] | null {
        return reader.readArray(reader => AssetDetailDto.deserializeCore(reader));
    }
}
