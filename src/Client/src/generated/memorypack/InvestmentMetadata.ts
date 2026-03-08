import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { InvestmentAccountType } from "./InvestmentAccountType";
import { IAssetMetadata } from "./IAssetMetadata";
import { Holding } from "./Holding";
import { VestingEvent } from "./VestingEvent";

export class InvestmentMetadata implements IAssetMetadata {
    accountType: InvestmentAccountType;
    holdings: (Holding | null)[] | null;
    grantDate: Date | null;
    totalSharesGranted: number | null;
    sharesVested: number | null;
    vestingSchedule: (VestingEvent | null)[] | null;
    ticker: string | null;
    grantPricePerShare: number | null;

    constructor() {
        this.accountType = 0;
        this.holdings = null;
        this.grantDate = null;
        this.totalSharesGranted = null;
        this.sharesVested = null;
        this.vestingSchedule = null;
        this.ticker = null;
        this.grantPricePerShare = null;

    }

    static serialize(value: InvestmentMetadata | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: InvestmentMetadata | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(8);
        writer.writeInt32(value.accountType);
        writer.writeArray(value.holdings, (writer, x) => Holding.serializeCore(writer, x));
        writer.writeNullableDate(value.grantDate);
        writer.writeNullableInt32(value.totalSharesGranted);
        writer.writeNullableInt32(value.sharesVested);
        writer.writeArray(value.vestingSchedule, (writer, x) => VestingEvent.serializeCore(writer, x));
        writer.writeString(value.ticker);
        writer.writeNullableFloat64(value.grantPricePerShare);

    }

    static serializeArray(value: (InvestmentMetadata | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (InvestmentMetadata | null)[] | null): void {
        writer.writeArray(value, (writer, x) => InvestmentMetadata.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): InvestmentMetadata | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): InvestmentMetadata | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new InvestmentMetadata();
        if (count == 8) {
            value.accountType = reader.readInt32();
            value.holdings = reader.readArray(reader => Holding.deserializeCore(reader));
            value.grantDate = reader.readNullableDate();
            value.totalSharesGranted = reader.readNullableInt32();
            value.sharesVested = reader.readNullableInt32();
            value.vestingSchedule = reader.readArray(reader => VestingEvent.deserializeCore(reader));
            value.ticker = reader.readString();
            value.grantPricePerShare = reader.readNullableFloat64();

        }
        else if (count > 8) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.accountType = reader.readInt32(); if (count == 1) return value;
            value.holdings = reader.readArray(reader => Holding.deserializeCore(reader)); if (count == 2) return value;
            value.grantDate = reader.readNullableDate(); if (count == 3) return value;
            value.totalSharesGranted = reader.readNullableInt32(); if (count == 4) return value;
            value.sharesVested = reader.readNullableInt32(); if (count == 5) return value;
            value.vestingSchedule = reader.readArray(reader => VestingEvent.deserializeCore(reader)); if (count == 6) return value;
            value.ticker = reader.readString(); if (count == 7) return value;
            value.grantPricePerShare = reader.readNullableFloat64(); if (count == 8) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (InvestmentMetadata | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (InvestmentMetadata | null)[] | null {
        return reader.readArray(reader => InvestmentMetadata.deserializeCore(reader));
    }
}
