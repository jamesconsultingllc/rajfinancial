import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class CryptoHolding {
    coinSymbol: string;
    coinName: string | null;
    quantity: number;
    costBasis: number | null;
    currentPrice: number | null;
    isStaking: boolean | null;
    stakingApy: number | null;

    constructor() {
        this.coinSymbol = "";
        this.coinName = null;
        this.quantity = 0;
        this.costBasis = null;
        this.currentPrice = null;
        this.isStaking = null;
        this.stakingApy = null;

    }

    static serialize(value: CryptoHolding | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: CryptoHolding | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(7);
        writer.writeString(value.coinSymbol);
        writer.writeString(value.coinName);
        writer.writeFloat64(value.quantity);
        writer.writeNullableFloat64(value.costBasis);
        writer.writeNullableFloat64(value.currentPrice);
        writer.writeNullableBoolean(value.isStaking);
        writer.writeNullableFloat64(value.stakingApy);

    }

    static serializeArray(value: (CryptoHolding | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (CryptoHolding | null)[] | null): void {
        writer.writeArray(value, (writer, x) => CryptoHolding.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): CryptoHolding | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): CryptoHolding | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new CryptoHolding();
        if (count == 7) {
            value.coinSymbol = reader.readString();
            value.coinName = reader.readString();
            value.quantity = reader.readFloat64();
            value.costBasis = reader.readNullableFloat64();
            value.currentPrice = reader.readNullableFloat64();
            value.isStaking = reader.readNullableBoolean();
            value.stakingApy = reader.readNullableFloat64();

        }
        else if (count > 7) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.coinSymbol = reader.readString(); if (count == 1) return value;
            value.coinName = reader.readString(); if (count == 2) return value;
            value.quantity = reader.readFloat64(); if (count == 3) return value;
            value.costBasis = reader.readNullableFloat64(); if (count == 4) return value;
            value.currentPrice = reader.readNullableFloat64(); if (count == 5) return value;
            value.isStaking = reader.readNullableBoolean(); if (count == 6) return value;
            value.stakingApy = reader.readNullableFloat64(); if (count == 7) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (CryptoHolding | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (CryptoHolding | null)[] | null {
        return reader.readArray(reader => CryptoHolding.deserializeCore(reader));
    }
}
