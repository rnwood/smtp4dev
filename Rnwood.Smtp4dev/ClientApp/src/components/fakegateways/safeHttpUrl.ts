/**
 * Returns the normalized absolute URL when the value is an absolute http: or https: URL, otherwise null.
 * Used for every link built from data received by the fake gateways (javascript:, data:, relative and
 * protocol-relative values must never become clickable).
 */
export function safeHttpUrl(value: string | null | undefined): string | null {
    if (typeof value !== "string") {
        return null;
    }

    const trimmed = value.trim();
    if (trimmed.length === 0) {
        return null;
    }

    try {
        // No base URL on purpose: relative and protocol-relative ("//evil") values throw.
        const url = new URL(trimmed);
        if (url.protocol === "http:" || url.protocol === "https:") {
            return url.href;
        }
    } catch {
        // not a valid absolute URL
    }

    return null;
}

export default safeHttpUrl;
