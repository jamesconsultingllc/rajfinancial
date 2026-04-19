using System.ComponentModel;

namespace RajFinancial.Shared.Entities.Storage;

/// <summary>
///     Cloud storage providers supported for document storage.
/// </summary>
public enum StorageProvider
{
    [Description("OneDrive — Microsoft 365 personal or business cloud storage.")]
    OneDrive = 0,

    [Description("Google Drive — Google Workspace or consumer cloud storage.")]
    GoogleDrive = 1,

    [Description("Dropbox — Dropbox personal or business cloud storage.")]
    Dropbox = 2,
}