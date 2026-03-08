import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class BeneficiaryAssignmentDto {
    beneficiaryId: string;
    beneficiaryName: string;
    relationship: string;
    allocationPercent: number;
    type: string;

    constructor() {
        this.beneficiaryId = "00000000-0000-0000-0000-000000000000";
        this.beneficiaryName = "";
        this.relationship = "";
        this.allocationPercent = 0;
        this.type = "";

    }

    static serialize(value: BeneficiaryAssignmentDto | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: BeneficiaryAssignmentDto | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(5);
        writer.writeGuid(value.beneficiaryId);
        writer.writeString(value.beneficiaryName);
        writer.writeString(value.relationship);
        writer.writeFloat64(value.allocationPercent);
        writer.writeString(value.type);

    }

    static serializeArray(value: (BeneficiaryAssignmentDto | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (BeneficiaryAssignmentDto | null)[] | null): void {
        writer.writeArray(value, (writer, x) => BeneficiaryAssignmentDto.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): BeneficiaryAssignmentDto | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): BeneficiaryAssignmentDto | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new BeneficiaryAssignmentDto();
        if (count == 5) {
            value.beneficiaryId = reader.readGuid();
            value.beneficiaryName = reader.readString();
            value.relationship = reader.readString();
            value.allocationPercent = reader.readFloat64();
            value.type = reader.readString();

        }
        else if (count > 5) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.beneficiaryId = reader.readGuid(); if (count == 1) return value;
            value.beneficiaryName = reader.readString(); if (count == 2) return value;
            value.relationship = reader.readString(); if (count == 3) return value;
            value.allocationPercent = reader.readFloat64(); if (count == 4) return value;
            value.type = reader.readString(); if (count == 5) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (BeneficiaryAssignmentDto | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (BeneficiaryAssignmentDto | null)[] | null {
        return reader.readArray(reader => BeneficiaryAssignmentDto.deserializeCore(reader));
    }
}
