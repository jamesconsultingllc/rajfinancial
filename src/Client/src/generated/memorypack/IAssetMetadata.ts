import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

import { VehicleMetadata } from "./VehicleMetadata"; 
import { RealEstateMetadata } from "./RealEstateMetadata"; 
import { InvestmentMetadata } from "./InvestmentMetadata"; 
import { RetirementMetadata } from "./RetirementMetadata"; 
import { BankAccountMetadata } from "./BankAccountMetadata"; 
import { InsuranceMetadata } from "./InsuranceMetadata"; 
import { BusinessMetadata } from "./BusinessMetadata"; 
import { PersonalPropertyMetadata } from "./PersonalPropertyMetadata"; 
import { CollectibleMetadata } from "./CollectibleMetadata"; 
import { CryptocurrencyMetadata } from "./CryptocurrencyMetadata"; 
import { IntellectualPropertyMetadata } from "./IntellectualPropertyMetadata"; 
import { OtherAssetMetadata } from "./OtherAssetMetadata"; 

export abstract class IAssetMetadata {
    static serialize(value: IAssetMetadata | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: IAssetMetadata | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }
        else if (value instanceof VehicleMetadata) {
            writer.writeUnionHeader(0);
            VehicleMetadata.serializeCore(writer, value);
            return;
        }
        else if (value instanceof RealEstateMetadata) {
            writer.writeUnionHeader(1);
            RealEstateMetadata.serializeCore(writer, value);
            return;
        }
        else if (value instanceof InvestmentMetadata) {
            writer.writeUnionHeader(2);
            InvestmentMetadata.serializeCore(writer, value);
            return;
        }
        else if (value instanceof RetirementMetadata) {
            writer.writeUnionHeader(3);
            RetirementMetadata.serializeCore(writer, value);
            return;
        }
        else if (value instanceof BankAccountMetadata) {
            writer.writeUnionHeader(4);
            BankAccountMetadata.serializeCore(writer, value);
            return;
        }
        else if (value instanceof InsuranceMetadata) {
            writer.writeUnionHeader(5);
            InsuranceMetadata.serializeCore(writer, value);
            return;
        }
        else if (value instanceof BusinessMetadata) {
            writer.writeUnionHeader(6);
            BusinessMetadata.serializeCore(writer, value);
            return;
        }
        else if (value instanceof PersonalPropertyMetadata) {
            writer.writeUnionHeader(7);
            PersonalPropertyMetadata.serializeCore(writer, value);
            return;
        }
        else if (value instanceof CollectibleMetadata) {
            writer.writeUnionHeader(8);
            CollectibleMetadata.serializeCore(writer, value);
            return;
        }
        else if (value instanceof CryptocurrencyMetadata) {
            writer.writeUnionHeader(9);
            CryptocurrencyMetadata.serializeCore(writer, value);
            return;
        }
        else if (value instanceof IntellectualPropertyMetadata) {
            writer.writeUnionHeader(10);
            IntellectualPropertyMetadata.serializeCore(writer, value);
            return;
        }
        else if (value instanceof OtherAssetMetadata) {
            writer.writeUnionHeader(11);
            OtherAssetMetadata.serializeCore(writer, value);
            return;
        }

        else {
            throw new Error("Concrete type is not in MemoryPackUnion");
        }
    }

    static serializeArray(value: IAssetMetadata[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: IAssetMetadata[] | null): void {
        writer.writeArray(value, (writer, x) => IAssetMetadata.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): IAssetMetadata | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): IAssetMetadata | null {
        const [ok, tag] = reader.tryReadUnionHeader();
        if (!ok) {
            return null;
        }

        switch (tag) {
            case 0:
                return VehicleMetadata.deserializeCore(reader);
            case 1:
                return RealEstateMetadata.deserializeCore(reader);
            case 2:
                return InvestmentMetadata.deserializeCore(reader);
            case 3:
                return RetirementMetadata.deserializeCore(reader);
            case 4:
                return BankAccountMetadata.deserializeCore(reader);
            case 5:
                return InsuranceMetadata.deserializeCore(reader);
            case 6:
                return BusinessMetadata.deserializeCore(reader);
            case 7:
                return PersonalPropertyMetadata.deserializeCore(reader);
            case 8:
                return CollectibleMetadata.deserializeCore(reader);
            case 9:
                return CryptocurrencyMetadata.deserializeCore(reader);
            case 10:
                return IntellectualPropertyMetadata.deserializeCore(reader);
            case 11:
                return OtherAssetMetadata.deserializeCore(reader);

            default:
                throw new Error("Tag is not found in this MemoryPackUnion");
        }
    }

    static deserializeArray(buffer: ArrayBuffer): (IAssetMetadata | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (IAssetMetadata | null)[] | null {
        return reader.readArray(reader => IAssetMetadata.deserializeCore(reader));
    }
}
