// ============================================================================
// RAJ Financial - DTO DateTime Wrapper
// ============================================================================
// A MemoryPack-compatible wrapper class that enables implicit conversion
// between DateTimeOffset (entities) and DateTime (DTOs), eliminating manual
// .UtcDateTime calls throughout the codebase.
//
// Why a class (not struct)?
//   [GenerateTypeScript] only supports classes. Using a class lets MemoryPack's
//   source generator auto-produce the TypeScript counterpart (DtoDateTime.ts)
//   alongside all other DTO types — no hand-maintained TS file required.
// ============================================================================

using MemoryPack;

namespace RajFinancial.Shared;

/// <summary>
///     A wrapper for <see cref="DateTime"/> that enables implicit conversion
///     to/from <see cref="DateTimeOffset"/> for seamless entity ↔ DTO mapping.
/// </summary>
/// <remarks>
///     <para>
///         <b>Problem:</b> Per project convention (see ASSET_TYPE_SPECIFICATIONS.md),
///         entities use <see cref="DateTimeOffset"/> for proper timezone handling,
///         while DTOs use <see cref="DateTime"/> for MemoryPack compatibility.
///         This requires manual <c>.UtcDateTime</c> calls at every mapping point.
///     </para>
///     <para>
///         <b>Solution:</b> This wrapper provides implicit operators so assignments
///         like <c>dto.CreatedAt = entity.CreatedAt</c> just work without explicit
///         conversion. The compiler handles it automatically.
///     </para>
///     <para>
///         <b>MemoryPack:</b> Serializes as a single <see cref="DateTime"/> field.
///         <c>[GenerateTypeScript]</c> auto-generates the TypeScript counterpart.
///     </para>
/// </remarks>
/// <example>
/// <code>
/// // Entity → DTO (implicit DateTimeOffset → DtoDateTime)
/// var dto = new AssetDto
/// {
///     CreatedAt = asset.CreatedAt,    // No .UtcDateTime needed
///     UpdatedAt = asset.UpdatedAt     // Nullable works too
/// };
/// 
/// // DTO → Entity (implicit DtoDateTime → DateTimeOffset)
/// entity.CreatedAt = dto.CreatedAt;   // No manual conversion
/// </code>
/// </example>
[MemoryPackable]
[GenerateTypeScript]
public sealed partial class DtoDateTime : IEquatable<DtoDateTime>, IComparable<DtoDateTime>
{
    /// <summary>
    ///     The underlying UTC DateTime value.
    /// </summary>
    [MemoryPackOrder(0)]
    public DateTime Value { get; init; }

    [MemoryPackConstructor]
    public DtoDateTime(DateTime value) => Value = value;

    public DtoDateTime() => Value = default;

    // =========================================================================
    // Implicit Conversions - Entity ↔ DTO
    // =========================================================================

    /// <summary>
    ///     Implicitly converts <see cref="DateTimeOffset"/> to <see cref="DtoDateTime"/>.
    ///     Used when mapping entity properties to DTO properties.
    /// </summary>
    public static implicit operator DtoDateTime(DateTimeOffset offset) =>
        new(offset.UtcDateTime);

    /// <summary>
    ///     Implicitly converts <see cref="DtoDateTime"/> to <see cref="DateTimeOffset"/>.
    ///     Used when mapping DTO properties back to entity properties.
    /// </summary>
    public static implicit operator DateTimeOffset(DtoDateTime dto) =>
        new(DateTime.SpecifyKind(dto.Value, DateTimeKind.Utc), TimeSpan.Zero);

    /// <summary>
    ///     Implicitly converts <see cref="DtoDateTime"/> to <see cref="DateTime"/>.
    ///     Allows direct use where DateTime is expected.
    /// </summary>
    public static implicit operator DateTime(DtoDateTime dto) => dto.Value;

    /// <summary>
    ///     Implicitly converts <see cref="DateTime"/> to <see cref="DtoDateTime"/>.
    ///     Allows assignment from DateTime values.
    /// </summary>
    public static implicit operator DtoDateTime(DateTime value) => new(value);

    // =========================================================================
    // IEquatable, IComparable
    // =========================================================================

    public bool Equals(DtoDateTime? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is DtoDateTime other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(DtoDateTime? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public static bool operator ==(DtoDateTime? left, DtoDateTime? right) =>
        left?.Equals(right) ?? right is null;
    public static bool operator !=(DtoDateTime? left, DtoDateTime? right) => !(left == right);

    // =========================================================================
    // ToString
    // =========================================================================

    public override string ToString() => Value.ToString("O"); // ISO 8601
}
