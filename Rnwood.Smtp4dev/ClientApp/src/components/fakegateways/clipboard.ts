import { ElNotification } from "element-plus";

export async function copyToClipboard(text: string): Promise<void> {
    try {
        await navigator.clipboard.writeText(text);
        ElNotification.success({ title: "Copied", message: "Copied to clipboard", duration: 1500 });
    } catch (e: any) {
        ElNotification.error({ title: "Copy failed", message: e?.message ?? String(e) });
    }
}

export function formatLocalTime(iso: string | null | undefined): string {
    if (!iso) {
        return "";
    }
    const date = new Date(iso);
    return isNaN(date.getTime()) ? iso : date.toLocaleString();
}
