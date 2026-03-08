import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { IAssetMetadata } from "./IAssetMetadata";

export class VehicleMetadata implements IAssetMetadata {
    vin: string | null;
    make: string;
    model: string;
    year: number;
    mileage: number | null;
    color: string | null;
    licensePlate: string | null;

    constructor() {
        this.vin = null;
        this.make = "";
        this.model = "";
        this.year = 0;
        this.mileage = null;
        this.color = null;
        this.licensePlate = null;

    }

    static serialize(value: VehicleMetadata | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: VehicleMetadata | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(7);
        writer.writeString(value.vin);
        writer.writeString(value.make);
        writer.writeString(value.model);
        writer.writeInt32(value.year);
        writer.writeNullableInt32(value.mileage);
        writer.writeString(value.color);
        writer.writeString(value.licensePlate);

    }

    static serializeArray(value: (VehicleMetadata | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (VehicleMetadata | null)[] | null): void {
        writer.writeArray(value, (writer, x) => VehicleMetadata.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): VehicleMetadata | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): VehicleMetadata | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new VehicleMetadata();
        if (count == 7) {
            value.vin = reader.readString();
            value.make = reader.readString();
            value.model = reader.readString();
            value.year = reader.readInt32();
            value.mileage = reader.readNullableInt32();
            value.color = reader.readString();
            value.licensePlate = reader.readString();

        }
        else if (count > 7) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.vin = reader.readString(); if (count == 1) return value;
            value.make = reader.readString(); if (count == 2) return value;
            value.model = reader.readString(); if (count == 3) return value;
            value.year = reader.readInt32(); if (count == 4) return value;
            value.mileage = reader.readNullableInt32(); if (count == 5) return value;
            value.color = reader.readString(); if (count == 6) return value;
            value.licensePlate = reader.readString(); if (count == 7) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (VehicleMetadata | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (VehicleMetadata | null)[] | null {
        return reader.readArray(reader => VehicleMetadata.deserializeCore(reader));
    }
}
