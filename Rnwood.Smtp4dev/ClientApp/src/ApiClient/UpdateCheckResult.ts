import GitHubRelease from "./GitHubRelease";

export default class UpdateCheckResult {
    updateAvailable: boolean;
    newReleases: GitHubRelease[];
    currentVersion: string;
    showWhatsNew: boolean;
    lastSeenVersion: string;
}
