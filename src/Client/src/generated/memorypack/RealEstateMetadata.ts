import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { PropertyType } from "./PropertyType";
import { IAssetMetadata } from "./IAssetMetadata";

export class RealEstateMetadata implements IAssetMetadata {
    address: string;
    address2: string | null;
    city: string;
    state: string;
    zipCode: string;
    country: string;
    propertyType: PropertyType;
    squareFeet: number | null;
    yearBuilt: number | null;
    lotSize: string | null;
    bedrooms: number | null;
    bathrooms: number | null;

    constructor() {
        this.address = "";
        this.address2 = null;
        this.city = "";
        this.state = "";
        this.zipCode = "";
        this.country = "";
        this.propertyType = 0;
        this.squareFeet = null;
        this.yearBuilt = null;
        this.lotSize = null;
        this.bedrooms = null;
        this.bathrooms = null;

    }

    static serialize(value: RealEstateMetadata | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: RealEstateMetadata | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(12);
        writer.writeString(value.address);
        writer.writeString(value.address2);
        writer.writeString(value.city);
        writer.writeString(value.state);
        writer.writeString(value.zipCode);
        writer.writeString(value.country);
        writer.writeInt32(value.propertyType);
        writer.writeNullableInt32(value.squareFeet);
        writer.writeNullableInt32(value.yearBuilt);
        writer.writeString(value.lotSize);
        writer.writeNullableInt32(value.bedrooms);
        writer.writeNullableFloat64(value.bathrooms);

    }

    static serializeArray(value: (RealEstateMetadata | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (RealEstateMetadata | null)[] | null): void {
        writer.writeArray(value, (writer, x) => RealEstateMetadata.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): RealEstateMetadata | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): RealEstateMetadata | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new RealEstateMetadata();
        if (count == 12) {
            value.address = reader.readString();
            value.address2 = reader.readString();
            value.city = reader.readString();
            value.state = reader.readString();
            value.zipCode = reader.readString();
            value.country = reader.readString();
            value.propertyType = reader.readInt32();
            value.squareFeet = reader.readNullableInt32();
            value.yearBuilt = reader.readNullableInt32();
            value.lotSize = reader.readString();
            value.bedrooms = reader.readNullableInt32();
            value.bathrooms = reader.readNullableFloat64();

        }
        else if (count > 12) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.address = reader.readString(); if (count == 1) return value;
            value.address2 = reader.readString(); if (count == 2) return value;
            value.city = reader.readString(); if (count == 3) return value;
            value.state = reader.readString(); if (count == 4) return value;
            value.zipCode = reader.readString(); if (count == 5) return value;
            value.country = reader.readString(); if (count == 6) return value;
            value.propertyType = reader.readInt32(); if (count == 7) return value;
            value.squareFeet = reader.readNullableInt32(); if (count == 8) return value;
            value.yearBuilt = reader.readNullableInt32(); if (count == 9) return value;
            value.lotSize = reader.readString(); if (count == 10) return value;
            value.bedrooms = reader.readNullableInt32(); if (count == 11) return value;
            value.bathrooms = reader.readNullableFloat64(); if (count == 12) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (RealEstateMetadata | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (RealEstateMetadata | null)[] | null {
        return reader.readArray(reader => RealEstateMetadata.deserializeCore(reader));
    }
}
