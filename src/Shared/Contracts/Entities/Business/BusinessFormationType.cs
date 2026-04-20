using System.ComponentModel;

namespace RajFinancial.Shared.Contracts.Entities.Business;

/// <summary>
///     Legal formation type for business entities.
/// </summary>
public enum BusinessFormationType
{
    [Description("Sole Proprietorship — unincorporated business owned by one individual; no legal separation from the owner.")]
    SoleProprietorship = 0,

    [Description("Single-Member LLC — one-owner limited liability company; disregarded entity for federal tax by default.")]
    SingleMemberLlc = 1,

    [Description("Multi-Member LLC — LLC with two or more owners; taxed as a partnership by default.")]
    MultiMemberLlc = 2,

    [Description("S-Corporation — pass-through entity that elected Subchapter S status (Form 2553).")]
    SCorporation = 3,

    [Description("C-Corporation — separately taxable corporation (default for Inc. without an S election).")]
    CCorporation = 4,

    [Description("General Partnership — two or more partners share management, profits, and unlimited liability.")]
    Partnership = 5,

    [Description("Limited Partnership — at least one general partner and one limited partner (LPs have liability shield).")]
    LimitedPartnership = 6,

    [Description("Non-Profit — organized for charitable, educational, or similar purposes; may qualify for 501(c) status.")]
    NonProfit = 7,
}