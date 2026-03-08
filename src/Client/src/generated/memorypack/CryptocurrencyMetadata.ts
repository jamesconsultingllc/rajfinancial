import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { CryptoWalletType } from "./CryptoWalletType";
import { IAssetMetadata } from "./IAssetMetadata";
import { CryptoHolding } from "./CryptoHolding";

export class CryptocurrencyMetadata implements IAssetMetadata {
    walletType: CryptoWalletType;
    holdings: (CryptoHolding | null)[] | null;

    constructor() {
        this.walletType = 0;
        this.holdings = null;

    }

    static serialize(value: CryptocurrencyMetadata | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: CryptocurrencyMetadata | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(2);
        writer.writeInt32(value.walletType);
        writer.writeArray(value.holdings, (writer, x) => CryptoHolding.serializeCore(writer, x));

    }

    static serializeArray(value: (CryptocurrencyMetadata | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (CryptocurrencyMetadata | null)[] | null): void {
        writer.writeArray(value, (writer, x) => CryptocurrencyMetadata.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): CryptocurrencyMetadata | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): CryptocurrencyMetadata | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new CryptocurrencyMetadata();
        if (count == 2) {
            value.walletType = reader.readInt32();
            value.holdings = reader.readArray(reader => CryptoHolding.deserializeCore(reader));

        }
        else if (count > 2) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.walletType = reader.readInt32(); if (count == 1) return value;
            value.holdings = reader.readArray(reader => CryptoHolding.deserializeCore(reader)); if (count == 2) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (CryptocurrencyMetadata | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (CryptocurrencyMetadata | null)[] | null {
        return reader.readArray(reader => CryptocurrencyMetadata.deserializeCore(reader));
    }
}
