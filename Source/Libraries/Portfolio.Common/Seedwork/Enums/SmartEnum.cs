using System.Reflection;

namespace Portfolio.Common.Seedwork.Enums;

public abstract class SmartEnum<TEnum> : IEquatable<SmartEnum<TEnum>>
    where TEnum : SmartEnum<TEnum>
{
    private static readonly Lazy<Dictionary<int, TEnum>> ById = new(
        () => GetAllFields().ToDictionary(e => e.Id));

    private static readonly Lazy<Dictionary<string, TEnum>> ByName = new(
        () => GetAllFields().ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase));

    public int Id { get; }
    public string Name { get; }

    protected SmartEnum(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public static IReadOnlyCollection<TEnum> GetAll() => ById.Value.Values.ToList().AsReadOnly();

    public static TEnum FromId(int id) =>
        ById.Value.TryGetValue(id, out var result)
            ? result
            : throw new ArgumentException($"No {typeof(TEnum).Name} with Id {id}");

    public static TEnum FromName(string name) =>
        ByName.Value.TryGetValue(name, out var result)
            ? result
            : throw new ArgumentException($"No {typeof(TEnum).Name} with Name '{name}'");

    public static bool TryFromId(int id, out TEnum? result) =>
        ById.Value.TryGetValue(id, out result);

    public static bool TryFromName(string name, out TEnum? result) =>
        ByName.Value.TryGetValue(name, out result);

    private static IEnumerable<TEnum> GetAllFields() =>
        typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(TEnum))
            .Select(f => (TEnum)f.GetValue(null)!);

    public bool Equals(SmartEnum<TEnum>? other) => other is not null && Id == other.Id;
    public override bool Equals(object? obj) => Equals(obj as SmartEnum<TEnum>);
    public override int GetHashCode() => Id.GetHashCode();
    public override string ToString() => Name;

    public static bool operator ==(SmartEnum<TEnum>? left, SmartEnum<TEnum>? right) =>
        Equals(left, right);

    public static bool operator !=(SmartEnum<TEnum>? left, SmartEnum<TEnum>? right) =>
        !Equals(left, right);
}
