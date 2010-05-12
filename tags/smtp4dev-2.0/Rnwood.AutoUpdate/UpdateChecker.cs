using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Forms;

namespace Rnwood.AutoUpdate
{
    public class UpdateChecker
    {
        public UpdateChecker(Uri releaseFileUrl, Version currentVersion)
        {
            ReleaseFileUrl = releaseFileUrl;
            CurrentVersion = currentVersion;
        }

        public Uri ReleaseFileUrl { get; private set; }
        public Version CurrentVersion { get; private set; }

        public bool CheckForUpdate(bool includePrerelease)
        {
            Release latestRelease = GetLatestRelease(includePrerelease);

            if (latestRelease != null && latestRelease.Version > CurrentVersion)
            {
                UpdateAvailableForm form = new UpdateAvailableForm(latestRelease, CurrentVersion);
                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        latestRelease.InitiateDownload();
                        Environment.Exit(0);
                    }
                    catch
                    {
                        MessageBox.Show(
                            "Failed to initiate download. Please check you have an active Internet connection and try again.",
                            "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }


                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        public Release GetLatestRelease(bool includePreRelease)
        {
            Release[] releases = GetReleases();
            return releases.FirstOrDefault(r => includePreRelease || r.status == releaseStatus.release);
        }

        private Release[] _releases;

        public Release[] GetReleases()
        {
            if (_releases == null)
            {
                WebClient webClient = new WebClient();
                using (Stream stream = webClient.OpenRead(ReleaseFileUrl))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(Releases));
                    Releases releases = ((Releases)ser.Deserialize(stream));
                    _releases = releases.Release.OrderByDescending(r => r.Version).ToArray();
                }
            }
            return _releases;
        }
    }
}
