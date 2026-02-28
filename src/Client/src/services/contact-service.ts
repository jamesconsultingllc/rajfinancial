/**
 * Contact service with mock CRUD and TanStack Query hooks.
 *
 * @description Mirrors the asset-service pattern. Uses in-memory
 * mock data until the API is ready. Swap `delay()` calls for
 * real `fetch()` when the backend endpoints exist.
 *
 * Query keys:
 * - ["contacts", params?]   → contact list
 * - ["contacts", id]        → single contact
 * - ["contact-links", id]   → asset links for a contact
 */
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import type {
  ContactDto,
  ContactType,
  CreateContactRequest,
  UpdateContactRequest,
  AssetContactLinkDto,
} from "@/types/contacts";
import { mockContacts, getContactAssetLinks } from "@/data/mock-contacts";

/** Simulate network delay. */
const delay = (ms = 400) => new Promise((r) => setTimeout(r, ms));

/* ------------------------------------------------------------------ */
/*  Raw service functions                                              */
/* ------------------------------------------------------------------ */

/**
 * Fetches all contacts, optionally filtered by type.
 *
 * @param params.type - Filter by ContactType.
 * @returns Promise resolving to an array of ContactDto.
 */
export async function getContacts(params?: {
  type?: ContactType;
}): Promise<ContactDto[]> {
  await delay();
  let result = [...mockContacts];
  if (params?.type) {
    result = result.filter((c) => c.contactType === params.type);
  }
  return result;
}

/**
 * Fetches a single contact by ID.
 *
 * @param id - Contact unique identifier.
 * @returns Promise resolving to a ContactDto.
 * @throws {Error} CONTACT_NOT_FOUND if ID doesn't match.
 */
export async function getContact(id: string): Promise<ContactDto> {
  await delay();
  const contact = mockContacts.find((c) => c.id === id);
  if (!contact) throw new Error("CONTACT_NOT_FOUND");
  return { ...contact };
}

/**
 * Creates a new contact from the discriminated request.
 *
 * @param data - CreateContactRequest (Individual | Trust | Organization).
 * @returns Promise resolving to the created ContactDto.
 */
export async function createContact(
  data: CreateContactRequest
): Promise<ContactDto> {
  await delay(600);
  const now = new Date().toISOString();

  const base: ContactDto = {
    id: crypto.randomUUID(),
    contactType: data.contactType,
    displayName: "",
    email: data.email,
    phone: data.phone,
    address: data.address,
    notes: data.notes,
    assetLinkCount: 0,
    createdAt: now,
    updatedAt: now,
  };

  switch (data.contactType) {
    case "Individual":
      base.displayName = `${data.firstName} ${data.lastName}`;
      base.firstName = data.firstName;
      base.lastName = data.lastName;
      base.dateOfBirth = data.dateOfBirth;
      base.ssn = data.ssn ? `••••${data.ssn.slice(-4)}` : undefined;
      base.ssnMasked = !!data.ssn;
      base.relationship = data.relationship;
      break;
    case "Trust":
      base.displayName = data.trustName;
      base.trustName = data.trustName;
      base.ein = data.ein ? `••••${data.ein.slice(-4)}` : undefined;
      base.einMasked = !!data.ein;
      base.category = data.category;
      base.purpose = data.purpose;
      base.specificType = data.specificType;
      base.trustDate = data.trustDate;
      base.stateOfFormation = data.stateOfFormation;
      base.isGrantorTrust = data.isGrantorTrust;
      base.hasCrummeyProvisions = data.hasCrummeyProvisions;
      base.isGstExempt = data.isGstExempt;
      break;
    case "Organization":
      base.displayName = data.organizationName;
      base.organizationName = data.organizationName;
      base.ein = data.ein ? `••••${data.ein.slice(-4)}` : undefined;
      base.einMasked = !!data.ein;
      base.organizationType = data.organizationType;
      base.is501C3 = data.is501C3;
      break;
  }

  mockContacts.push(base);
  return base;
}

/**
 * Updates an existing contact (partial update).
 *
 * @param id - Contact unique identifier.
 * @param data - Fields to update. ContactType cannot be changed.
 * @returns Promise resolving to the updated ContactDto.
 * @throws {Error} CONTACT_NOT_FOUND if ID doesn't match.
 */
export async function updateContact(
  id: string,
  data: UpdateContactRequest
): Promise<ContactDto> {
  await delay(600);
  const idx = mockContacts.findIndex((c) => c.id === id);
  if (idx === -1) throw new Error("CONTACT_NOT_FOUND");

  const existing = mockContacts[idx];
  const updated: ContactDto = {
    ...existing,
    ...data,
    updatedAt: new Date().toISOString(),
  };

  // Recompute display name
  switch (updated.contactType) {
    case "Individual":
      updated.displayName = `${updated.firstName ?? ""} ${updated.lastName ?? ""}`.trim();
      break;
    case "Trust":
      updated.displayName = updated.trustName ?? updated.displayName;
      break;
    case "Organization":
      updated.displayName = updated.organizationName ?? updated.displayName;
      break;
  }

  mockContacts[idx] = updated;
  return updated;
}

/**
 * Deletes a contact. Fails if the contact has active asset links.
 *
 * @param id - Contact unique identifier.
 * @throws {Error} CONTACT_NOT_FOUND if ID doesn't match.
 * @throws {Error} CONTACT_HAS_LINKS if contact has active asset links.
 */
export async function deleteContact(id: string): Promise<void> {
  await delay(400);
  const idx = mockContacts.findIndex((c) => c.id === id);
  if (idx === -1) throw new Error("CONTACT_NOT_FOUND");

  // Enforce RESTRICT: cannot delete contact with active links
  const links = getContactAssetLinks(id);
  if (links.length > 0) {
    throw new Error("CONTACT_HAS_LINKS");
  }

  mockContacts.splice(idx, 1);
}

/**
 * Fetches asset links for a specific contact.
 *
 * @param contactId - Contact unique identifier.
 * @returns Promise resolving to an array of AssetContactLinkDto.
 */
export async function getContactLinks(
  contactId: string
): Promise<AssetContactLinkDto[]> {
  await delay(300);
  return getContactAssetLinks(contactId);
}

/* ------------------------------------------------------------------ */
/*  TanStack Query hooks                                               */
/* ------------------------------------------------------------------ */

/**
 * Fetches contacts list with optional type filter.
 *
 * @param params.type - Optional ContactType filter.
 * @returns TanStack Query result with ContactDto[].
 */
export function useContacts(params?: { type?: ContactType }) {
  return useQuery({
    queryKey: ["contacts", params],
    queryFn: () => getContacts(params),
  });
}

/**
 * Fetches a single contact by ID.
 *
 * @param id - Contact unique identifier.
 * @returns TanStack Query result with ContactDto.
 */
export function useContact(id: string) {
  return useQuery({
    queryKey: ["contacts", id],
    queryFn: () => getContact(id),
    enabled: !!id,
  });
}

/**
 * Fetches asset links for a specific contact.
 *
 * @param contactId - Contact unique identifier.
 * @returns TanStack Query result with AssetContactLinkDto[].
 */
export function useContactLinks(contactId: string) {
  return useQuery({
    queryKey: ["contact-links", contactId],
    queryFn: () => getContactLinks(contactId),
    enabled: !!contactId,
  });
}

/**
 * Mutation hook for creating a new contact.
 *
 * @returns TanStack Query mutation for CreateContactRequest → ContactDto.
 */
export function useCreateContact() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: createContact,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["contacts"] }),
  });
}

/**
 * Mutation hook for updating an existing contact.
 *
 * @returns TanStack Query mutation for { id, data } → ContactDto.
 */
export function useUpdateContact() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateContactRequest }) =>
      updateContact(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["contacts"] }),
  });
}

/**
 * Mutation hook for deleting a contact.
 *
 * @description Will fail with CONTACT_HAS_LINKS if the contact
 * has active asset-contact links (RESTRICT delete behavior).
 *
 * @returns TanStack Query mutation for id → void.
 */
export function useDeleteContact() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: deleteContact,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["contacts"] }),
  });
}
