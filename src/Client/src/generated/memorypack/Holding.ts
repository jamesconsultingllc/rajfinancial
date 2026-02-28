import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { HoldingType } from "./HoldingType";

export class Holding {
    ticker: string;
    name: string | null;
    holdingType: HoldingType;
    shares: number;
    costBasis: number | null;
    currentPrice: number | null;

    constructor() {
        this.ticker = "";
        this.name = null;
        this.holdingType = 0;
        this.shares = 0;
        this.costBasis = null;
        this.currentPrice = null;

    }

    static serialize(value: Holding | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: Holding | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(6);
        writer.writeString(value.ticker);
        writer.writeString(value.name);
        writer.writeInt32(value.holdingType);
        writer.writeFloat64(value.shares);
        writer.writeNullableFloat64(value.costBasis);
        writer.writeNullableFloat64(value.currentPrice);

    }

    static serializeArray(value: (Holding | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (Holding | null)[] | null): void {
        writer.writeArray(value, (writer, x) => Holding.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): Holding | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): Holding | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new Holding();
        if (count == 6) {
            value.ticker = reader.readString();
            value.name = reader.readString();
            value.holdingType = reader.readInt32();
            value.shares = reader.readFloat64();
            value.costBasis = reader.readNullableFloat64();
            value.currentPrice = reader.readNullableFloat64();

        }
        else if (count > 6) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.ticker = reader.readString(); if (count == 1) return value;
            value.name = reader.readString(); if (count == 2) return value;
            value.holdingType = reader.readInt32(); if (count == 3) return value;
            value.shares = reader.readFloat64(); if (count == 4) return value;
            value.costBasis = reader.readNullableFloat64(); if (count == 5) return value;
            value.currentPrice = reader.readNullableFloat64(); if (count == 6) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (Holding | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (Holding | null)[] | null {
        return reader.readArray(reader => Holding.deserializeCore(reader));
    }
}
