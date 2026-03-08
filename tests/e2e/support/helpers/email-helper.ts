// ============================================================================
// Email Verification Helper
// ============================================================================
// Retrieves verification codes from emails via IMAP.
// Ported from C# AcceptanceTests/Helpers/TestEmailHelper.cs
// ============================================================================

import { ImapFlow } from "imapflow";
import { simpleParser } from "mailparser";
import { config } from "../config";

const TEST_EMAIL_DOMAIN = "rajlegacy.org";

/**
 * Generates a unique test email address for rajlegacy.org.
 * Pattern: test-e2e-{timestamp}-{guid}@rajlegacy.org
 */
export function generateTestEmail(): string {
  const timestamp = new Date()
    .toISOString()
    .replace(/[-:T]/g, "")
    .slice(0, 14);
  const guid = Math.random().toString(36).substring(2, 10);
  return `test-e2e-${timestamp}-${guid}@${TEST_EMAIL_DOMAIN}`;
}

/**
 * Whether IMAP settings are configured.
 */
export function isImapConfigured(): boolean {
  return !!(config.imap.host && config.imap.username && config.imap.password);
}

/**
 * Retrieves the verification code from the most recent email via IMAP.
 * Polls the inbox every 3 seconds until a matching email arrives.
 *
 * @param recipientEmail - Email address to filter by (To: header)
 * @param timeoutSeconds - Maximum time to wait for the email
 * @returns The extracted verification code
 */
export async function getVerificationCodeFromEmail(
  recipientEmail: string,
  timeoutSeconds = 120
): Promise<string> {
  if (!isImapConfigured()) {
    throw new Error(
      "IMAP settings not configured. " +
        "Set IMAP_HOST, IMAP_PORT, IMAP_USERNAME, IMAP_PASSWORD in .env"
    );
  }

  const client = new ImapFlow({
    host: config.imap.host!,
    port: config.imap.port,
    secure: true,
    auth: {
      user: config.imap.username!,
      pass: config.imap.password!,
    },
    logger: false,
  });

  const startTime = Date.now();

  console.log(`📧 Connecting to IMAP: ${config.imap.host}:${config.imap.port}`);
  await client.connect();

  try {
    const lock = await client.getMailboxLock("INBOX");
    try {
      while ((Date.now() - startTime) / 1000 < timeoutSeconds) {
        // Notify server to check for new messages (critical for polling)
        await client.noop();

        const code = await tryFindVerificationCode(
          client,
          recipientEmail
        );
        if (code) {
          console.log(`✓ Found verification code: ${code}`);
          return code;
        }

        const elapsed = Math.round((Date.now() - startTime) / 1000);
        console.log(
          `⏳ Waiting for email to ${recipientEmail}... (${elapsed}s elapsed)`
        );
        await new Promise((r) => setTimeout(r, 3000));
      }
    } finally {
      lock.release();
    }
  } finally {
    await client.logout();
  }

  throw new Error(
    `Email not received within ${timeoutSeconds} seconds for ${recipientEmail}`
  );
}

async function tryFindVerificationCode(
  client: ImapFlow,
  recipientEmail: string
): Promise<string | null> {
  // Search for recent messages (last 5 minutes)
  const cutoff = new Date(Date.now() - 5 * 60 * 1000);

  const uids: number[] = [];
  for await (const msg of client.fetch(
    { since: cutoff },
    { envelope: true, source: true }
  )) {
    uids.push(msg.uid);
  }

  if (uids.length === 0) return null;

  // Check newest first
  for (const uid of uids.reverse()) {
    const msg = await client.fetchOne(
      uid,
      { source: true },
      { uid: true }
    );

    if (!msg || !msg.source) continue;

    const parsed = await simpleParser(msg.source as Buffer);

    // Filter by recipient
    const toAddresses = Array.isArray(parsed.to) ? parsed.to : [parsed.to];
    const isForRecipient = toAddresses.some((addr) =>
      addr?.value?.some(
        (v) => v.address?.toLowerCase() === recipientEmail.toLowerCase()
      )
    );

    if (!isForRecipient) continue;

    console.log(`📧 Found email: ${parsed.subject} (${parsed.date})`);

    const body = parsed.text || parsed.html || "";
    const code = extractVerificationCode(body);
    if (code) return code;
  }

  return null;
}

/**
 * Extracts a verification code from email body text.
 * Handles common Entra External ID patterns.
 */
function extractVerificationCode(body: string): string | null {
  // Common patterns:
  // "Verification code: 123456"
  // "Your code is: 123456"
  // "Enter code: 123456"
  // "OTP: 123456"
  const patterns = [
    /(?:verification|verify|confirmation)\s*(?:code|number)[\s:]*(\d{4,8})/i,
    /(?:your|the)\s*code\s*(?:is)?[\s:]*(\d{4,8})/i,
    /(?:enter|use)\s*(?:this)?\s*code[\s:]*(\d{4,8})/i,
    /(?:otp|one.?time)[\s:]*(\d{4,8})/i,
    /(?:code|pin)[\s:]+(\d{4,8})/i,
    // Standalone 6-8 digit code on its own line
    /^\s*(\d{6,8})\s*$/m,
  ];

  for (const pattern of patterns) {
    const match = body.match(pattern);
    if (match?.[1]) return match[1];
  }

  return null;
}
